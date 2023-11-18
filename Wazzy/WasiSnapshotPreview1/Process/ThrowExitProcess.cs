using Wasmtime;

namespace Wazzy.WasiSnapshotPreview1.Process;

/// <summary>
/// Throws a `ThrowExitProcessException` when proc_exit is called
/// </summary>
public class ThrowExitProcess
    : IWasiExitProcess
{
    public void ProcExit(Caller caller, uint code)
    {
        throw new ThrowExitProcessException(code);
    }
}

public class ThrowExitProcessException(uint exitCode)
    : Exception
{
    public uint ExitCode { get; } = exitCode;
}