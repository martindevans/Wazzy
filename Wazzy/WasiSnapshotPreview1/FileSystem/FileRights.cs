namespace Wazzy.WasiSnapshotPreview1.FileSystem;

[Flags]
public enum FileRights
    : long
{
    None = 0,

    DataSync = 1 << 0,
    FdRead = 1 << 1,
    FdSeek = 1 << 2,
    FdStatSet = 1 << 3,
    FdSync = 1 << 4,
    FdTell = 1 << 5,
    FdWrite = 1 << 6,
    FdAdvise = 1 << 7,
    FdAllocate = 1 << 8,
    PathCreateDirectory = 1 << 9,
    PathCreateFile = 1 << 10,
    PathLinkSource = 1 << 11,
    PathLinkTarget = 1 << 12,
    PathOpen = 1 << 13,
    FdReadDir = 1 << 14,
    PathReadLink = 1 << 15,
    PathRenameSource = 1 << 16,
    PathRenameTarget = 1 << 17,
    PathFilestatGet = 1 << 18,
    PathFilestatSetSize = 1 << 19,
    PathFilestatSetTimes = 1 << 20,
    FdFilestatGet = 1 << 21,
    FdFilestatSetSize = 1 << 22,
    FdFilestatSetTimes = 1 << 23,
    PathSymlink = 1 << 24,
    PathRemoveDirectory = 1 << 25,
    PathUnlinkFile = 1 << 26,
    PollFdReadWrite = 1 << 27,
    SockShutdown = 1 << 28,
    SockAccept = 1 << 29,

    All = -1,
}