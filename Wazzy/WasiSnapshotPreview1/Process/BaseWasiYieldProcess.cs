using Wasmtime;

namespace Wazzy.WasiSnapshotPreview1.Process;

public abstract class BaseWasiYieldProcess
    : IWasiFeature
{
    /// <summary>
    /// The module which the exports of this feature are defined in
    /// </summary>
    public static readonly string Module = "wasi_snapshot_preview1";

    /// <summary>
    /// Yield execution immediately
    /// </summary>
    /// <returns></returns>
    protected abstract WasiError SchedulerYield(Caller caller);

    public void DefineOn(Linker linker)
    {
        linker.DefineFunction(Module, "sched_yield", caller => (int)SchedulerYield(
            caller
        ));
    }
}