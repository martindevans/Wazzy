using Wasmtime;

namespace Wazzy.WasiSnapshotPreview1.Process;

/// <summary>
/// Throws a `ThrowExitProcessException` when proc_exit is called
/// </summary>
public class ThrowExitProcess
    : IWasiExitProcess
{
    public void ProcExit(Caller caller, int code)
    {
        throw new ThrowExitProcessException(code);
    }
}

public class ThrowExitProcessException(int exitCode)
    : Exception
{
    public int ExitCode { get; } = exitCode;
}