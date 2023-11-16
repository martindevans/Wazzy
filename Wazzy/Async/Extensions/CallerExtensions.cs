using Wasmtime;

namespace Wazzy.Async.Extensions;

public static class CallerExtensions
{
    /// <summary>
    /// Get the current async state of the WASM instance behind this caller.
    /// </summary>
    /// <param name="caller"></param>
    /// <returns></returns>
    public static AsyncState? GetAsyncState(this Caller caller)
    {
        return (AsyncState?)caller.GetFunction("asyncify_get_state")
                                 ?.WrapFunc<int>()
                                 ?.Invoke();
    }

    internal static Memory GetDefaultMemory(this Caller caller)
    {
        // Get memory, it should always be called "memory" (by convention)
        return caller.GetMemory("memory")
            ?? throw new InvalidOperationException("Cannot get exported memory");
    }

    internal static void AsyncifyStartUnwind(this Caller caller, int addr)
    {
        caller.GetAsyncState().AssertState(AsyncState.None);
        caller.GetFunction("asyncify_start_unwind")!.WrapAction<int>()!.Invoke(addr);
        caller.GetAsyncState().AssertState(AsyncState.Suspending);
    }

    internal static void AsyncifyStopRewind(this Caller caller)
    {
        caller.GetAsyncState().AssertState(AsyncState.Resuming);
        caller.GetFunction("asyncify_stop_rewind")!.WrapAction()!.Invoke();
        caller.GetAsyncState().AssertState(AsyncState.None);
    }
}