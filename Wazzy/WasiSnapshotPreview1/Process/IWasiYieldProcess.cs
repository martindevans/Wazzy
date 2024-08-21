using Wasmtime;

namespace Wazzy.WasiSnapshotPreview1.Process;

public interface IWasiYieldProcess
    : IWasiFeature
{
    /// <summary>
    /// The module which the exports of this feature are defined in
    /// </summary>
    public const string Module = "wasi_snapshot_preview1";

    /// <summary>
    /// Yield execution immediately
    /// </summary>
    /// <returns></returns>
    protected WasiError SchedulerYield(Caller caller);

    void IWasiFeature.DefineOn(Linker linker)
    {
        linker.DefineFunction(Module, "sched_yield", caller => (int)SchedulerYield(
            caller
        ));
    }
}