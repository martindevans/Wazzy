using Wasmtime;
using Wazzy.Async.Extensions;
using Wazzy.Extensions;

namespace Wazzy.Async;

public static class WasmAsyncExtensions
{
    // Save 256KB space for stack unwinding
    internal const int StashSize = 1024 * 256;

    // These "stashes" are used to store memory from the time unwinding/rewinding starts to when it finishes.
    // This is a very quick operation, just as long as it takes to walk up/down the WASM stack.
    private static readonly ThreadLocal<SavedStackData?> _unwindStash = new(false);
    private static readonly ThreadLocal<SavedStackData?> _rewindStash = new(false);

    #region low level state transitions
    /// <summary>
    /// Stop unwinding and retrieve the suspended WASM call stack.
    /// This must be called after a WASM method was called by C# and has returned to C# due to an async suspend.
    /// </summary>
    /// <param name="instance"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public static SavedStack StopUnwind(this Instance instance)
    {
        // Finish the async unwind
        Func<int>? getter = null;
        instance.AsyncifyStopUnwind(ref getter);

        // Grab stashed data saved at start of unwind
        var savedStackData = _unwindStash.Value;
        _unwindStash.Value = null;
        if (savedStackData == null)
            throw new InvalidOperationException("StopUnwind cannot be called when there is no unwind in progress");

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
        stack.CheckEpoch();

        // Check state is as expected
        Func<int>? getter = null;
        instance.GetAsyncState(ref getter).AssertState(AsyncState.None);

        // Put the stash somewhere that it can be found when the rewind is complete
        _rewindStash.Value = stack.Data;

        // Create the memory map to locate the correct address
        var state = new AsyncMemoryState(stack.Data.AllocatedBufferAddress ?? 0, StashSize);

        // Trigger async rewind
        instance.AsyncifyStartRewind(state.GetRewindStructAddress(), ref getter);
    }
    #endregion

    #region high level API
    /// <summary>
    /// Try to get saved locals state, returns null if not unwinding
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="caller"></param>
    /// <returns></returns>
    public static T? GetSuspendedLocals<T>(this Caller caller)
        where T : unmanaged
    {
        // If we're not rewinding then there's nothing to restore.
        if (caller.GetAsyncState() != AsyncState.Resuming)
            return null;

        // Get memory state
        var memory = caller.GetDefaultMemory();
        var state = new AsyncMemoryState(_rewindStash.Value!.AllocatedBufferAddress ?? 0, StashSize);

        // Grab the data from where it should be in memory
        return state.ReadLocals<T>(memory);
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
        // Check state is as expected
        Func<int>? getter = null;
        caller.GetAsyncState(ref getter).AssertState(AsyncState.None);

        // Get some things we need
        var memory = caller.GetDefaultMemory();
        int localSize;
        unsafe { localSize = sizeof(T); }

        // Allocate state object
        var stash = SavedStackData.Get();
        _unwindStash.Value = stash;

        // Get a buffer, there are two ways to do this:
        // - Ask the wasm code to malloc a buffer
        // - Failing that, copy out the first chunk of memory to a temporary stash, and use that space for unwinding
        var allocated = caller.AsyncifyMallocBuffer(StashSize);
        AsyncMemoryState state;
        if (allocated.HasValue)
        {
            state = new AsyncMemoryState(allocated.Value, StashSize);
            stash.AllocatedBufferAddress = allocated;
        }
        else
        {
            // No buffer could be allocated, read out memory into stash so we can use that
            memory.ReadMemory(stash.Data);
            state = new AsyncMemoryState(0, StashSize);
        }

        // Write the execution state number
        state.WriteExecutionStateNumber(memory, executionState);

        // setup the rewind structure
        state.WriteRewindStruct(memory, localSize);

        // Start async unwinding into memory
        caller.AsyncifyStartUnwind(state.GetRewindStructAddress(), ref getter);

        // Copy locals into memory
        state.WriteLocals(memory, localSize, locals);

        // Increment state number
        state.IncrementStateNumber(memory);
        
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
        Func<int>? getter = null;
        if (caller.GetAsyncState(ref getter) != AsyncState.Resuming)
        {
            executionState = 0;
            return 0;
        }

        // Get the saved stash data
        var saved = _rewindStash.Value;
        if (saved == null)
            throw new InvalidOperationException("Cannot StopRewind when not rewind is in progress");
        _rewindStash.Value = null;

        // Get memory state
        var memory = caller.GetDefaultMemory();
        var state = new AsyncMemoryState(saved.AllocatedBufferAddress ?? 0, StashSize);

        // Read the execution state from memory
        executionState = state.ReadExecutionStateNumber(memory);

        // Stop the async rewind
        caller.AsyncifyStopRewind(ref getter);

        // Handle buffer cleanup
        if (saved.AllocatedBufferAddress.HasValue)
        {
            // Free the buffer lent to use by client code
            caller.AsyncifyFreeBuffer(saved.AllocatedBufferAddress.Value);
        }
        else
        {
            // Restore the stashed memory we copied out
            memory.WriteMemory(saved.Data);
        }
        SavedStackData.Return(saved);

        return executionState;
    }
    #endregion
}