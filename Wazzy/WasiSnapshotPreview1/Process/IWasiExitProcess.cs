using Wasmtime;

namespace Wazzy.WasiSnapshotPreview1.Process;

public interface IWasiExitProcess
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
    protected void ProcExit(Caller caller, int code);

    void IWasiFeature.DefineOn(Linker linker)
    {
        linker.DefineFunction(Module, "proc_exit", (Caller caller, int code) => ProcExit(
            caller,
            code
        ));
    }
}