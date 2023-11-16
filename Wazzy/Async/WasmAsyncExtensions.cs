using System.Runtime.CompilerServices;
using Wasmtime;
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

    #region get memory
    private static Memory GetMemory(Instance instance)
    {
        // Get memory, it should always be called "memory" (by convention)
        return instance.GetMemory("memory")
            ?? throw new InvalidOperationException("Cannot get exported memory");
    }

    private static Memory GetMemory(Caller caller)
    {
        // Get memory, it should always be called "memory" (by convention)
        return caller.GetMemory("memory")
            ?? throw new InvalidOperationException("Cannot get exported memory");
    }
    #endregion

    #region memory addresses
    /// <summary>
    /// Data is packed into memory, starting at this address. Fields are:
    ///  - 4 bytes: execution state index
    ///  - 4 bytes: size of locals data
    ///  - N bytes: locals data
    ///  - (padding to 8 byte alignment)
    ///  - 16 bytes: async stack structure
    ///  - {stack data...}
    /// </summary>
    private const int BaseAddress = 16;

    private static int GetExecutionStateAddr()
    {
        return BaseAddress;
    }

    private static int GetLocalsSizeAddr()
    {
        return BaseAddress + 4;
    }

    private static int GetLocalsAddr()
    {
        return GetLocalsSizeAddr() + 4;
    }

    private static int GetAsyncStackStructAddr(int localsSize)
    {
        // Get the address
        var x = GetLocalsAddr() + localsSize;

        // Move up to the next address aligned to 8
        var addr = x + 7 & -8;

        return addr;
    }

    private static int GetAsyncStackStartAddr(int localsSize)
    {
        var stackStruct = GetAsyncStackStructAddr(localsSize);
        return stackStruct + 16;
    }

    private static ref AsyncStackStruct32 GetAsyncStackStruct32(Memory memory, int localsSize)
    {
        unsafe
        {
            var addr = GetAsyncStackStructAddr(localsSize);
            var ptr = memory.GetPointer() + addr;
            return ref *((AsyncStackStruct32*)ptr.ToPointer());
        }
    }

    private static ref AsyncStackStruct64 GetAsyncStackStruct64(Memory memory, int localsSize)
    {
        unsafe
        {
            var addr = GetAsyncStackStructAddr(localsSize);
            var ptr = memory.GetPointer() + addr;
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
    private static void StartUnwind(Caller caller, int executionState, int localSize)
    {
        var memory = GetMemory(caller);

        // The unwinding data structures are differently sized when 64 bit, so that's not supported for now
        if (memory.Is64Bit)
            throw new NotSupportedException("Cannot unwind 64 bit WASM");

        // Check state is as expected
        caller.GetAsyncState().AssertState(AsyncState.None);

        // Copy memory into stash to free up some space
        memory.ReadMemory(_unwindStash.Value);

        // Write the execution state number
        memory.WriteInt32(GetExecutionStateAddr(), executionState);

        // Set up rewind structure (start and end of asyncify stack)
        ref var stackStruct = ref GetAsyncStackStruct32(memory, localSize);
        stackStruct.StackStart = GetAsyncStackStartAddr(localSize);
        stackStruct.StackEnd = StashSize;

        // Start async unwinding into memory
        caller.AsyncifyStartUnwind(GetAsyncStackStructAddr(localSize));
    }

    /// <summary>
    /// Stop unwinding and retrieve the suspended WASM call stack
    /// </summary>
    /// <param name="instance"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public static SavedStack StopUnwind(Instance instance)
    {
        var memory = GetMemory(instance);

        // The unwinding data structures are differently sized when 64 bit, so that's not supported for now
        if (memory.Is64Bit)
            throw new NotSupportedException("Cannot unwind 64 bit WASM");

        // Finish the async unwind
        instance.AsyncifyStopUnwind();

        // Copy rewind stack out to C# array
        var savedStackData = SavedStackData.Get();
        memory.ReadMemory(savedStackData.Data);

        // Read execution state _before_ restoring memory state
        savedStackData.ExecutionState = memory.ReadInt32(GetExecutionStateAddr());
        savedStackData.LocalsSize = memory.ReadInt32(GetLocalsSizeAddr());

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
    public static void StartRewind(Instance instance, SavedStack stack)
    {
        if (stack.Value == null)
            throw new ArgumentException("Stack is null", nameof(stack));

        var memory = GetMemory(instance);

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
        instance.AsyncifyStartRewind(GetAsyncStackStructAddr(stack.LocalsSize));
    }

    /// <summary>
    /// Stop rewinding into a WASM stack and continue execution normally
    /// </summary>
    /// <param name="caller"></param>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    private static void StopRewind(Caller caller)
    {
        var memory = GetMemory(caller);

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
    public static T? GetSuspendedLocals<T>(this Caller caller, out int executionState)
        where T : unmanaged
    {
        // If we're not rewinding then there's nothing to restore.
        if (caller.GetAsyncState() != AsyncState.Resuming)
        {
            executionState = 0;
            return null;
        }

        var memory = GetMemory(caller);

        // Sanity check the size of the locals data
        int localSize;
        unsafe { localSize = sizeof(T); }
        if (memory.ReadInt32(GetLocalsSizeAddr()) != localSize)
            throw new InvalidOperationException($"Attempted to read locals data {typeof(T).Name}, but size does not match! Wrong type?");

        // Grab the data from where it should be in memory
        executionState = GetMemory(caller).ReadInt32(GetExecutionStateAddr());
        return memory.Read<T>(GetLocalsAddr());
    }

    /// <summary>
    /// Try to get saved locals state, returns null if not unwinding
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="caller"></param>
    /// <returns></returns>
    public static T? GetSuspendedLocals<T>(this Caller caller)
        where T : unmanaged
    {
        return caller.GetSuspendedLocals<T>(out _);
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
        var memory = GetMemory(caller);

        int localSize;
        unsafe { localSize = sizeof(T); }

        // Begin unwinding stack
        StartUnwind(caller, executionState, localSize);

        // Copy locals into memory
        memory.WriteInt32(GetLocalsSizeAddr(), localSize);
        memory.Write(GetLocalsAddr(), locals);

        // Increment state number
        var stateAddr = GetExecutionStateAddr();
        var state = memory.ReadInt32(stateAddr);
        memory.WriteInt32(stateAddr, state + 1);
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
        executionState = GetMemory(caller).ReadInt32(GetExecutionStateAddr());

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
    /// Throw an exception indicating that the execution state was unpexpected
    /// </summary>
    /// <param name="executionState"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static Exception BadExecutionState(int executionState, [CallerMemberName] string name = "")
    {
        return new BadExecutionStateException(executionState, name);
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