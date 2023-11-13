using Wasmtime;

namespace Wazzy.WasiSnapshotPreview1.Process;

public abstract class BaseWasiProcess
    : IWasiFeature
{
    /// <summary>
    /// The module which the exports of this feature are defined in
    /// </summary>
    public static readonly string Module = "wasi_snapshot_preview1";

    public abstract void ProcExit(Caller caller, uint code);

    /// <summary>
    /// Yield execution immediately
    /// </summary>
    /// <returns></returns>
    public abstract WasiError SchedulerYield(Caller caller);

    public void DefineOn(Linker linker)
    {
        linker.DefineFunction(Module, "proc_exit", (Caller caller, int code) => ProcExit(
            caller,
            unchecked((uint)code)
        ));

        linker.DefineFunction(Module, "sched_yield", caller => (int)SchedulerYield(
            caller
        ));
    }
}