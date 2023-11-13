using System.Runtime.InteropServices;

namespace Wazzy.WasiSnapshotPreview1.FileSystem;

/// <summary>
/// Information about an open file descriptor
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct FdStat
{
    public readonly FileType FileType;
    public readonly FdFlags Flags;
    public readonly FileRights RightsBase;
    public readonly FileRights RightsInheriting;

    public FdStat(FileType fileType, FdFlags flags, FileRights rightsBase = FileRights.All, FileRights rightsInheriting = FileRights.All)
    {
        FileType = fileType;
        Flags = flags;
        RightsBase = rightsBase;
        RightsInheriting = rightsInheriting;
    }
}