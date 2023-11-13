using System.Runtime.InteropServices;

namespace Wazzy.WasiSnapshotPreview1.FileSystem;

public enum PreopenType
    : byte
{
    Directory,
}

/// <summary>
/// Information about a "preopened" item in the filesystem (e.g. file system roots)
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Prestat
{
    public readonly PreopenType Type;
    public readonly int NameLength;

    public Prestat(PreopenType type, int nameLength)
    {
        Type = type;
        NameLength = nameLength;
    }
}