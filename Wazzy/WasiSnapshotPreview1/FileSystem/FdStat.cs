using System.Runtime.InteropServices;

namespace Wazzy.WasiSnapshotPreview1.FileSystem;

/// <summary>
/// Information about an open file descriptor
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct FdStat(FileType fileType, FdFlags flags, FileRights rightsBase = FileRights.All, FileRights rightsInheriting = FileRights.All)
{
    public readonly FileType FileType = fileType;
    public readonly FdFlags Flags = flags;
    public readonly FileRights RightsBase = rightsBase;
    public readonly FileRights RightsInheriting = rightsInheriting;
}