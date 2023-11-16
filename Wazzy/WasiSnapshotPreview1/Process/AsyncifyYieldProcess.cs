using Wasmtime;
using Wazzy.Async;
using Wazzy.Async.Extensions;

namespace Wazzy.WasiSnapshotPreview1.Process;

/// <summary>
/// When "SchedulerYield" is called this will suspend the process using the binaryen asyncify transform
/// </summary>
public class AsyncifyYieldProcess
    : BaseWasiYieldProcess
{
    protected override WasiError SchedulerYield(Caller caller)
    {
        // Immediately exit if this caller can't async suspend
        if (!caller.IsAsyncCapable())
            return WasiError.ENOTCAPABLE;

        // If this is the first time suspend execution, otherwise resume
        if (caller.GetAsyncState() == AsyncState.Resuming)
            caller.Resume();
        else
            caller.Suspend(0);

        return WasiError.SUCCESS;
    }
}