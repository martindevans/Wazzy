using Wasmtime;
using Wazzy.Interop;

namespace Wazzy.WasiSnapshotPreview1.Random;

public abstract class BaseWasiRandomSource
    : IWasiFeature
{
    /// <summary>
    /// The module which the exports of this feature are defined in
    /// </summary>
    public static readonly string Module = "wasi_snapshot_preview1";

    /// <summary>
    /// Fill a buffer with random data
    /// </summary>
    /// <param name="caller">Context for this call</param>
    /// <param name="output">Buffer to fill with random data</param>
    /// <returns></returns>
    public abstract WasiError RandomGet(Caller caller, Span<byte> output);

    public void DefineOn(Linker linker)
    {
        linker.DefineFunction(Module, "random_get",
            (Caller caller, int bufferAddr, int bufferSize) => (int)RandomGet(
                caller,
                new Buffer<byte>(bufferAddr, unchecked((uint)bufferSize)).GetSpan(caller)
            )
        );
    }
}