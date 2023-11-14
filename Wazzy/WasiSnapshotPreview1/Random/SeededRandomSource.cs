using System.Security.Cryptography;
using Wasmtime;

namespace Wazzy.WasiSnapshotPreview1.Random;

/// <summary>
/// Provides random numbers from a seeded System.Random instance
/// </summary>
public class SeededRandomSource
    : BaseWasiRandomSource
{
    private readonly System.Random _rng;

    public SeededRandomSource(int seed)
    {
        _rng = new System.Random(seed);
    }

    public SeededRandomSource()
        : this(RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue))
    {
    }

    protected override WasiError RandomGet(Caller caller, Span<byte> output)
    {
        lock (_rng)
        {
            _rng.NextBytes(output);
        }
            
        return WasiError.SUCCESS;
    }
}