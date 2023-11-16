using Wasmtime;

namespace Wazzy.WasiSnapshotPreview1.Process;

public abstract class BaseWasiExitProcess
    : IWasiFeature
{
    /// <summary>
    /// The module which the exports of this feature are defined in
    /// </summary>
    public static readonly string Module = "wasi_snapshot_preview1";

    /// <summary>
    /// Exit the process with the given return code
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="code"></param>
    protected abstract void ProcExit(Caller caller, uint code);

    public void DefineOn(Linker linker)
    {
        linker.DefineFunction(Module, "proc_exit", (Caller caller, int code) => ProcExit(
            caller,
            unchecked((uint)code)
        ));
    }
}