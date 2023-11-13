using Microsoft.VisualBasic.FileIO;
using System.Runtime.InteropServices;

namespace Wazzy.WasiSnapshotPreview1.FileSystem;

/// <summary>
/// Information about an item in the filesystem
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct FileStat
{
    public readonly ulong Device;
    public readonly ulong Inode;
    public readonly FileType FileType;
    public readonly ulong LinkCount;
    public readonly ulong Size;

    public readonly ulong AccessTime;
    public readonly ulong ModifyTime;
    public readonly ulong ChangeTime;

    public FileStat(ulong dev, ulong inode, FileType type, ulong links, ulong size, ulong atime, ulong mtime, ulong ctime)
    {
        Device = dev;
        Inode = inode;
        FileType = type;
        LinkCount = links;
        Size = size;
        AccessTime = atime;
        ModifyTime = mtime;
        ChangeTime = ctime;
    }
}