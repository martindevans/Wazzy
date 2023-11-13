﻿using System.Security.Cryptography;
using Wasmtime;

namespace Wazzy.WasiSnapshotPreview1.Random;

public class CryptoRandomSource
    : BaseWasiRandomSource
{
    public override WasiError RandomGet(Caller caller, Span<byte> output)
    {
        RandomNumberGenerator.Fill(output);
        return WasiError.SUCCESS;
    }
}