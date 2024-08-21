using System.Text;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Files;

namespace Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Directories;

public class MappedZipEntryDirectoryContent
        : IDirectory
{
    private class Handle
        : BaseDirectoryHandle
    {
        private readonly MappedZipEntryDirectoryContent _dir;

        public Handle(MappedZipEntryDirectoryContent dir)
        {
            _dir = dir;
        }

        public override void Dispose()
        {
        }

        public override IDirectory Directory => _dir;

        public override IReadOnlyList<DirectoryItem> EnumerateChildren(ulong timestamp)
        {
            return _dir.EnumerateChildren();
        }

        protected override ulong? TryGetChildCount()
        {
            return null;
        }
    }

    private readonly MappedZipArchiveDirectoryContent _archiveRoot;
    private readonly string _selfPath;
    private readonly byte[] _selfPathBytes;

    public MappedZipEntryDirectoryContent(MappedZipArchiveDirectoryContent archiveRoot, string selfPath)
    {
        _archiveRoot = archiveRoot;
        _selfPath = selfPath.TrimEnd('/');
        _selfPathBytes = Encoding.UTF8.GetBytes(_selfPath);
    }

    public bool CanMove => false;
    public FileType FileType => FileType.Directory;

    public ulong AccessTime
    {
        get => _archiveRoot.AccessTime;
        set => _archiveRoot.AccessTime = value;
    }
    public ulong ModificationTime
    {
        get => _archiveRoot.ModificationTime;
        set => _archiveRoot.ModificationTime = value;
    }
    public ulong ChangeTime
    {
        get => _archiveRoot.ChangeTime;
        set => _archiveRoot.ChangeTime = value;
    }

    public void Delete()
    {
        throw new NotSupportedException();
    }

    public IFilesystemEntry ToInMemory()
    {
        throw new NotSupportedException();
    }

    public DirectoryItem? GetChild(ReadOnlySpan<byte> relativePath)
    {
        Span<byte> buffer = stackalloc byte[_selfPathBytes.Length + 1 + relativePath.Length];
        _selfPathBytes.CopyTo(buffer);
        buffer[_selfPathBytes.Length] = (byte)'/';
        relativePath.CopyTo(buffer[(_selfPathBytes.Length + 1)..]);

        return _archiveRoot.GetChild(buffer);
    }

    public IFilesystemHandle Open()
    {
        return new Handle(this);
    }

    #region writing (not supported)
    public (DirectoryItem?, PathOpenResult) Create(ReadOnlySpan<byte> name, IFilesystemEntry content)
    {
        throw new NotSupportedException("Mapped ZipArchive directory is not writable");
    }

    public (DirectoryItem?, PathOpenResult) CreateFile(ReadOnlySpan<byte> name, IFile? content = null)
    {
        throw new NotSupportedException("Mapped ZipArchive directory is not writable");
    }

    public (DirectoryItem?, WasiError) CreateDirectory(ReadOnlySpan<byte> name, IDirectory? content = null)
    {
        throw new NotSupportedException("Mapped ZipArchive directory is not writable");
    }

    public bool Add(ReadOnlySpan<byte> name, IFilesystemEntry child)
    {
        throw new NotSupportedException("Mapped ZipArchive directory is not writable");
    }

    public bool Delete(ReadOnlySpan<byte> name)
    {
        throw new NotSupportedException("Mapped ZipArchive directory is not writable");
    }

    public WasiError Move(ReadOnlySpan<byte> currentName, IDirectory destinationDirectory, ReadOnlySpan<byte> destinationName)
    {
        throw new NotSupportedException("Mapped ZipArchive directory is not writable");
    }
    #endregion

    #region helpers
    private IReadOnlyList<DirectoryItem>? _enumerateCache;

    private IReadOnlyList<DirectoryItem> EnumerateChildren()
    {
        return _enumerateCache ??= _archiveRoot.EnumerateChildren(_selfPath);
    }
    #endregion
}