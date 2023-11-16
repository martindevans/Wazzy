﻿using Wasmtime;
using Wazzy.Async.Extensions;
using Wazzy.Extensions;

namespace Wazzy.Async;

public static class WasmAsyncExtensions
{
    // Save 512KB space for stack unwinding
    internal const int StashSize = 1024 * 512;

    // These "stashes" are used to store memory from the time unwinding/rewinding starts to when it finishes.
    // This is a very quick operation, just as long as it takes to walk up/down the WASM stack.
    private static readonly ThreadLocal<byte[]> _unwindStash = new(() => new byte[StashSize]);
    private static readonly ThreadLocal<byte[]> _rewindStash = new(() => new byte[StashSize]);

    #region memory addresses
    /// <summary>
    /// Data is packed into memory, starting at this address. Fields are:
    /// - 4 bytes: execution state index
    /// - 4 bytes: size of locals data
    /// - 16 bytes: async stack structure
    /// - N bytes: locals data
    /// - (padding to 8 byte alignment)
    /// - {stack data...}
    /// </summary>
    private const int BaseAddress = 16;
    private const int ExecutionStateAddr = BaseAddress;
    private const int LocalsSizeAddr = ExecutionStateAddr + 4;
    private const int AsyncStackStructAddr = LocalsSizeAddr + 4;
    private const int LocalsDataAddr = AsyncStackStructAddr + 16;

    private static int GetAsyncStackStartAddr(int localsSize)
    {
        var x = LocalsDataAddr + localsSize;

        // Move up to the next address aligned to 8
        var addr = x + 7 & -8;

        return addr;
    }

    private static ref AsyncStackStruct32 GetAsyncStackStruct32(Memory memory)
    {
        unsafe
        {
            var ptr = memory.GetPointer() + AsyncStackStructAddr;
            return ref *((AsyncStackStruct32*)ptr.ToPointer());
        }
    }

    private static ref AsyncStackStruct64 GetAsyncStackStruct64(Memory memory)
    {
        unsafe
        {
            var ptr = memory.GetPointer() + AsyncStackStructAddr;
            return ref *((AsyncStackStruct64*)ptr.ToPointer());
        }
    }
    #endregion

    #region low level state transitions
    /// <summary>
    /// Unwind out of a WASM context, must call `StopUnwind` as soon as execution returns to C#
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="executionState">Execution state value to save into memory</param>
    /// <param name="localSize">Size of the locals data</param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    private static void StartUnwind(this Caller caller, int executionState, int localSize)
    {
        var memory = caller.GetDefaultMemory();

        // The unwinding data structures are differently sized when 64 bit, so that's not supported for now
        if (memory.Is64Bit)
            throw new NotSupportedException("Cannot unwind 64 bit WASM");

        // Check state is as expected
        caller.GetAsyncState().AssertState(AsyncState.None);

        // Copy memory into stash to free up some space
        memory.ReadMemory(_unwindStash.Value);

        // Write the execution state number
        memory.WriteInt32(ExecutionStateAddr, executionState);

        // Set up rewind structure (start and end of asyncify stack)
        ref var stackStruct = ref GetAsyncStackStruct32(memory);
        stackStruct.StackStart = GetAsyncStackStartAddr(localSize);
        stackStruct.StackEnd = StashSize;

        // Start async unwinding into memory
        caller.AsyncifyStartUnwind(AsyncStackStructAddr);
    }

    /// <summary>
    /// Stop unwinding and retrieve the suspended WASM call stack
    /// </summary>
    /// <param name="instance"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public static SavedStack StopUnwind(this Instance instance)
    {
        var memory = instance.GetDefaultMemory();

        // The unwinding data structures are differently sized when 64 bit, so that's not supported for now
        if (memory.Is64Bit)
            throw new NotSupportedException("Cannot unwind 64 bit WASM");

        // Finish the async unwind
        instance.AsyncifyStopUnwind();

        // Copy rewind stack out to C# array
        var savedStackData = SavedStackData.Get();
        memory.ReadMemory(savedStackData.Data);

        // Restore memory to the correct state
        memory.WriteMemory(_unwindStash.Value);

        return new SavedStack(savedStackData);
    }

    /// <summary>
    /// Restore a saved WASM callstack
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="stack"></param>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public static void StartRewind(this Instance instance, SavedStack stack)
    {
        if (stack.IsNull)
            throw new ArgumentException("Stack is null", nameof(stack));

        var memory = instance.GetDefaultMemory();

        // The unwinding data structures are differently sized when 64 bit, so that's not supported for now
        if (memory.Is64Bit)
            throw new NotSupportedException("Cannot unwind 64 bit WASM");

        // Check state is as expected
        instance.GetAsyncState().AssertState(AsyncState.None);

        // Copy out whatever is in memory into stash
        memory.ReadMemory(_rewindStash.Value);

        // Write rewind stack into free space
        memory.WriteMemory(stack.Value);

        // Trigger async rewind
        instance.AsyncifyStartRewind(AsyncStackStructAddr);

        // Dispose the stack, ensuring it cannot be used again
        stack.Dispose();
    }

    /// <summary>
    /// Stop rewinding into a WASM stack and continue execution normally
    /// </summary>
    /// <param name="caller"></param>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    private static void StopRewind(this Caller caller)
    {
        var memory = caller.GetDefaultMemory();

        // The unwinding data structures are differently sized when 64 bit, so that's not supported for now
        if (memory.Is64Bit)
            throw new NotSupportedException("Cannot unwind 64 bit WASM");

        // Stop the async rewind
        caller.AsyncifyStopRewind();

        // Restore stashed memory
        memory.WriteMemory(_rewindStash.Value);
    }
    #endregion

    #region high level API
    /// <summary>
    /// Try to get saved locals state, returns null if not unwinding
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="caller"></param>
    /// <param name="executionState">The current execution state</param>
    /// <returns></returns>
    public static T? GetSuspendedLocals<T>(this Caller caller)
        where T : unmanaged
    {
        // If we're not rewinding then there's nothing to restore.
        if (caller.GetAsyncState() != AsyncState.Resuming)
            return null;

        var memory = caller.GetDefaultMemory();

        // Sanity check the size of the locals data
        int localSize;
        unsafe { localSize = sizeof(T); }
        if (memory.ReadInt32(LocalsSizeAddr) != localSize)
            throw new InvalidOperationException($"Attempted to read locals data {typeof(T).Name}, but size does not match! Wrong type?");

        // Grab the data from where it should be in memory
        return memory.Read<T>(LocalsDataAddr);
    }

    /// <summary>
    /// Suspend execution, to be resumed later.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="caller"></param>
    /// <param name="locals">The locals to save, can be restored when resuming with GetSuspendedLocals()</param>
    /// <param name="executionState">The `executionState` that was previously output from `Resume`</param>
    public static void Suspend<T>(this Caller caller, T locals, int executionState)
        where T : unmanaged
    {
        var memory = caller.GetDefaultMemory();

        int localSize;
        unsafe { localSize = sizeof(T); }

        // Begin unwinding stack
        StartUnwind(caller, executionState, localSize);

        // Copy locals into memory
        memory.WriteInt32(LocalsSizeAddr, localSize);
        memory.Write(LocalsDataAddr, locals);

        // Increment state number
        var state = memory.ReadInt32(ExecutionStateAddr);
        memory.WriteInt32(ExecutionStateAddr, state + 1);
    }

    /// <summary>
    /// Suspend execution, to be resumed later. No locals data is saved
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="executionState">The `executionState` that was previously output from `Resume`</param>
    public static void Suspend(this Caller caller, int executionState)
    {
        Suspend(caller, default(Empty), executionState);
    }

    /// <summary>
    /// Resume execution that was previously suspended.
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="executionState">The execution state, increments every time this function resumes.</param>
    /// <returns>The execution state, increments every time this function resumes.</returns>
    public static int Resume(this Caller caller, out int executionState)
    {
        // If not rewinding just return 0, the first step in execution. Maybe we'll suspend later.
        if (caller.GetAsyncState() != AsyncState.Resuming)
        {
            executionState = 0;
            return 0;
        }

        // Read the execution state from memory
        executionState = caller.GetDefaultMemory().ReadInt32(ExecutionStateAddr);

        // Finish rewinding, ready to resume execution
        StopRewind(caller);

        return executionState;
    }

    /// <summary>
    /// Resume execution that was previously suspended.
    /// </summary>
    /// <param name="caller"></param>
    /// <returns>The execution state, increments every time this function resumes.</returns>
    public static int Resume(this Caller caller)
    {
        return caller.Resume(out _);
    }

    /// <summary>
    /// Check if the given caller is capable of async suspension/resumption
    /// </summary>
    /// <param name="caller"></param>
    /// <returns></returns>
    public static bool IsAsyncCapable(this Caller caller)
    {
        return caller.GetFunction("asyncify_start_unwind") != null;
    }
    #endregion
}