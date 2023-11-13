using System.Runtime.InteropServices;

namespace Wazzy.WasiSnapshotPreview1.FileSystem;

/// <summary>
/// A handle to an open file
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 4)]
public readonly record struct FileDescriptor
{
    [FieldOffset(0)]
    public readonly int Handle;

    public FileDescriptor(int handle)
    {
        Handle = handle;
    }

    public override string ToString()
    {
        return Handle.ToString();
    }
}