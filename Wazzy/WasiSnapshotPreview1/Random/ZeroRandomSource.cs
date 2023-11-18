using Wasmtime;

namespace Wazzy.WasiSnapshotPreview1.Random;

/// <summary>
/// Always provides zero when asked for "random" numbers
/// </summary>
public class ZeroRandomSource
    : IWasiRandomSource
{
    public WasiError RandomGet(Caller caller, Span<byte> output)
    {
        output.Clear();
        return WasiError.SUCCESS;
    }
}