using Wasmtime;

namespace Wazzy.Async.Extensions;

public static class InstanceExtensions
{
    internal static AsyncState? GetAsyncState(this Instance instance, ref Func<int>? getter)
    {
        if (getter == null)
        {
            getter = instance.GetFunction("asyncify_get_state")?.WrapFunc<int>();

            if (getter == null)
                return default;
        }

        return (AsyncState)getter();
    }

    /// <summary>
    /// Get the current async state of this instance
    /// </summary>
    /// <param name="instance"></param>
    /// <returns></returns>
    public static AsyncState? GetAsyncState(this Instance instance)
    {
        Func<int>? _ = null;
        return instance.GetAsyncState(ref _);
    }

    /// <summary>
    /// Check if the given Instance is capable of async suspension/resumption
    /// </summary>
    /// <param name="instance"></param>
    /// <returns></returns>
    public static bool IsAsyncCapable(this Instance instance)
    {
        return instance.GetFunction("asyncify_start_unwind") != null;
    }

    internal static void AsyncifyStopUnwind(this Instance instance, ref Func<int>? getter)
    {
        instance.GetAsyncState(ref getter).AssertState(AsyncState.Suspending);
        instance.GetFunction("asyncify_stop_unwind")!.WrapAction()!.Invoke();

#if DEBUG
        instance.GetAsyncState(ref getter).AssertState(AsyncState.None);
#endif
    }

    internal static void AsyncifyStartRewind(this Instance instance, int addr, ref Func<int>? getter)
    {
        instance.GetAsyncState(ref getter).AssertState(AsyncState.None);
        instance.GetFunction("asyncify_start_rewind")!.WrapAction<int>()!(addr);

#if DEBUG
        instance.GetAsyncState(ref getter).AssertState(AsyncState.Resuming);
#endif
    }
}