using System.Runtime.InteropServices;

namespace Wazzy.WasiSnapshotPreview1.FileSystem;

/// <summary>
/// Information about an item in the filesystem
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct FileStat(ulong dev, ulong inode, FileType type, ulong links, ulong size, ulong atime, ulong mtime, ulong ctime)
{
    public readonly ulong Device = dev;
    public readonly ulong Inode = inode;
    public readonly FileType FileType = type;
    public readonly ulong LinkCount = links;
    public readonly ulong Size = size;

    public readonly ulong AccessTime = atime;
    public readonly ulong ModifyTime = mtime;
    public readonly ulong ChangeTime = ctime;
}