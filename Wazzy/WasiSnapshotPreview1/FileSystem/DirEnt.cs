using System.Runtime.InteropServices;

namespace Wazzy.WasiSnapshotPreview1.FileSystem;

/// <summary>
/// Information about an item in a directory
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct DirEnt
{
    /// <summary>
    /// "Cookie" for the next item in sequence
    /// </summary>
    public ulong Next;

    /// <summary>
    /// INode of this item
    /// </summary>
    public ulong INode;

    /// <summary>
    /// Length of the name (encoded in UTF8) for this item
    /// </summary>
    public uint NameLength;

    /// <summary>
    /// Type of this item
    /// </summary>
    public FileType Type;
}