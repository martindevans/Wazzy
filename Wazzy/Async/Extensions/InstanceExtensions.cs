using Wasmtime;

namespace Wazzy.Async.Extensions;

public static class InstanceExtensions
{
    public static AsyncState? GetAsyncState(this Instance instance)
    {
        return (AsyncState?)instance.GetFunction("asyncify_get_state")
                                   ?.WrapFunc<int>()
                                   ?.Invoke();
    }

    internal static Memory GetDefaultMemory(this Instance instance)
    {
        // Get memory, it should always be called "memory" (by convention)
        return instance.GetMemory("memory")
            ?? throw new InvalidOperationException("Cannot get exported memory");
    }

    internal static void AsyncifyStopUnwind(this Instance instance)
    {
        instance.GetAsyncState().AssertState(AsyncState.Suspending);
        instance.GetFunction("asyncify_stop_unwind")!.WrapAction()!.Invoke();
        instance.GetAsyncState().AssertState(AsyncState.None);
    }

    internal static void AsyncifyStartRewind(this Instance instance, int addr)
    {
        instance.GetAsyncState().AssertState(AsyncState.None);
        instance.GetFunction("asyncify_start_rewind")!.WrapAction<int>()!(addr);
        instance.GetAsyncState().AssertState(AsyncState.Resuming);
    }
}