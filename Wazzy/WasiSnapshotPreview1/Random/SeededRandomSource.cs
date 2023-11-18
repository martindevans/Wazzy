using Wasmtime;

namespace Wazzy.WasiSnapshotPreview1.Random;

/// <summary>
/// Provides random numbers from a seeded System.Random instance
/// </summary>
public class SeededRandomSource(int seed)
    : IWasiRandomSource
{
    private readonly System.Random _rng = new(seed);

    public WasiError RandomGet(Caller caller, Span<byte> output)
    {
        lock (_rng)
        {
            _rng.NextBytes(output);
        }
            
        return WasiError.SUCCESS;
    }
}