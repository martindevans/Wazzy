namespace Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem;

public interface IFilesystemHandle
    : IDisposable
{
    IFilesystemEntry Content { get; }

    FileType FileType { get; }

    FileStat GetFileStat();

    FdStat GetStat();

    WasiError SetFlags(FdFlags flags);

    public WasiError SetTimes(ulong timestamp, long atime, long mtime, FstFlags fstFlags)
    {
        return Content.SetTimes(timestamp, atime, mtime, fstFlags);
    }
}