using System.Security.Cryptography;
using Wasmtime;

namespace Wazzy.WasiSnapshotPreview1.Random;

/// <summary>
/// Provides cryptographically strong random numbers
/// </summary>
public class CryptoRandomSource
    : IWasiRandomSource
{
    public WasiError RandomGet(Caller caller, Span<byte> output)
    {
        RandomNumberGenerator.Fill(output);
        return WasiError.SUCCESS;
    }
}