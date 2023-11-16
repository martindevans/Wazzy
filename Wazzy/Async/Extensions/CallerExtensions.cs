﻿using Wasmtime;

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
        var func = caller.GetFunction("asyncify_get_state")?.WrapFunc<int>();
        if (func == null)
            return null;

        return (AsyncState)func();
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