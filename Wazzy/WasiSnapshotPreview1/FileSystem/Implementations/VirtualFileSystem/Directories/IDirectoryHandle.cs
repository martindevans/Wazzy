namespace Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Directories;

public interface IDirectoryHandle
    : IFilesystemHandle
{
    IDirectory Directory { get; }

    IReadOnlyList<DirectoryItem> EnumerateChildren(ulong timestamp);
}

internal abstract class BaseDirectoryHandle
    : IDirectoryHandle
{
    public abstract IDirectory Directory { get; }

    public IFilesystemEntry Content => Directory;
    public FileType FileType => FileType.Directory;

    public FileStat GetFileStat()
    {
        return new FileStat(0, 0,
            FileType,
            0,
            TryGetChildCount() ?? (ulong)EnumerateChildren(0).Count,
            Directory.AccessTime,
            Directory.ModificationTime,
            Directory.ChangeTime
        );
    }

    protected abstract ulong? TryGetChildCount();

    public FdStat GetStat()
    {
        return new FdStat(FileType.Directory, FdFlags.None);
    }

    public WasiError SetFlags(FdFlags flags)
    {
        return WasiError.EISDIR;
    }

    public abstract IReadOnlyList<DirectoryItem> EnumerateChildren(ulong timestamp);

    public abstract void Dispose();
}