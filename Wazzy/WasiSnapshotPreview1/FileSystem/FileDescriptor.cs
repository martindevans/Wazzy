using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Wazzy.WasiSnapshotPreview1.FileSystem;

/// <summary>
/// A handle to an open file
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 4)]
[DebuggerDisplay("{" + nameof(Handle) + "}")]
public readonly struct FileDescriptor(int handle)
{
    [FieldOffset(0)]
    public readonly int Handle = handle;
}