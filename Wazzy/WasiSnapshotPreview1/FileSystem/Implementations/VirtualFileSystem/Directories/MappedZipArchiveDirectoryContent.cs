using System.IO.Compression;
using System.Text;
using Wazzy.Extensions;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Files;

namespace Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Directories;

public class MappedZipArchiveDirectoryContent
        : IDirectory
{
    private class Handle
        : BaseDirectoryHandle
    {
        private readonly MappedZipArchiveDirectoryContent _dir;

        public Handle(MappedZipArchiveDirectoryContent parent)
        {
            _dir = parent;
        }

        public override void Dispose()
        {
        }

        public override IDirectory Directory => _dir;

        public override IReadOnlyList<DirectoryItem> EnumerateChildren(ulong timestamp)
        {
            return _dir.EnumerateRootChildren();
        }

        protected override ulong? TryGetChildCount()
        {
            return null;
        }
    }

    private readonly ZipArchive _archive;
    private readonly Dictionary<string, DirectoryItem?> _childCache = new();
    private IReadOnlyList<DirectoryItem>? _rootEnumerationCache;

    public bool CanMove => false;

    public FileType FileType => FileType.Directory;

    public ulong AccessTime { get; set; }
    public ulong ModificationTime { get; set; }
    public ulong ChangeTime { get; set; }

    private readonly bool _contentCaching;

    public MappedZipArchiveDirectoryContent(string archivePath, IVFSClock clock, bool contentCaching)
        : this(File.OpenRead(archivePath), clock)
    {
        _contentCaching = contentCaching;
    }

    public MappedZipArchiveDirectoryContent(Stream archiveStream, IVFSClock clock)
    {
        _archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, false);

        AccessTime = ModificationTime = ChangeTime = clock.GetTime();
    }

    public IFilesystemEntry ToInMemory()
    {
        throw new NotSupportedException();
    }

    public DirectoryItem? GetChild(ReadOnlySpan<byte> relativePath)
    {
        var path = Encoding.UTF8.GetString(relativePath);
        var name = Path.GetFileName(path);

        if (!_childCache.TryGetValue(path, out var item))
        {
            // Try to find a file with this exact name
            var file = _archive.GetEntry(path);

            if (file != null && !file.FullName.EndsWith("/"))
            {
                item = new DirectoryItem(name, GetOrCreateFileByEntry(file.FullName));
            }
            else
            {
                var subitem = _archive.Entries.FirstOrDefault(e => e.FullName.StartsWith(path + "/"));
                if (subitem != null)
                    item = new DirectoryItem(name, GetOrCreateDirectoryByRelativePath(path));
                else
                    item = null;
            }

            _childCache[path] = item;
        }

        return item;
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

    public IReadOnlyList<DirectoryItem> EnumerateRootChildren()
    {
        if (_rootEnumerationCache != null)
            return _rootEnumerationCache;

        var discoveredDirectories = new HashSet<string>();
        var result = new List<DirectoryItem>();
        _rootEnumerationCache = result;
        foreach (var entry in _archive.Entries)
        {
            entry.FullName.AsSpan().Split('/', out var left, out _);

            // If it did not split at all then there were no separators, therefore it's a file in the root directory
            if (left.Length == entry.FullName.Length)
            {
                if (!_childCache.TryGetValue(entry.FullName, out var item))
                {
                    item = new DirectoryItem(entry.FullName, GetOrCreateFileByEntry(entry.FullName));
                    _childCache[entry.FullName] = item;
                }
                result.Add(item!.Value);
            }
            else
            {
                // Since it did split it must start with a directory in the root
                var dName = new string(left);
                if (discoveredDirectories.Add(dName))
                {
                    if (!_childCache.TryGetValue(dName, out var item))
                    {
                        item = new DirectoryItem(dName, GetOrCreateDirectoryByRelativePath(dName));
                        _childCache[dName] = item;
                    }
                    result.Add(item!.Value);
                }
            }
        }

        return _rootEnumerationCache;
    }

    public IReadOnlyList<DirectoryItem> EnumerateChildren(string prefix)
    {
        // There are a few cases here:
        // - An entry may be a file or a folder
        // - Every file in the archive **will** have an entry
        // - An empty folder **will** have an entry
        // - A folder will contents **may** have an entry

        if (!prefix.EndsWith('/'))
            prefix += "/";

        var discoveredDirectories = new HashSet<string>();
        return _archive
              .Entries
              .Where(e => e.FullName.StartsWith(prefix))
              .Select(Item)
              .Where(i => i.HasValue)
              .Select(i => i!.Value)
              .ToList();

        DirectoryItem? Item(ZipArchiveEntry entry)
        {
            var name = entry.FullName.TrimStart(prefix);
            if (name is "/" or "")
                return null;

            // If there's a directory separator then this is either an explicit directory, or a file in an implicit subdirectory
            if (name.Contains("/"))
            {
                name.AsSpan().Split('/', out var left, out _);
                var dName = new string(left);

                if (!discoveredDirectories.Add(dName))
                    return null;

                return new DirectoryItem(dName, GetOrCreateDirectoryByRelativePath($"{prefix}{dName}"));
            }

            return new DirectoryItem(name, GetOrCreateFileByEntry(entry.FullName));
        }
    }

    public ZipArchiveEntry? GetEntry(string entryName)
    {
        return _archive.GetEntry(entryName);
    }

    #region caching
    private readonly Dictionary<string, MappedZipEntryFile> _fileEntryCache = new();

    private MappedZipEntryFile GetOrCreateFileByEntry(string entry)
    {
        if (!_fileEntryCache.TryGetValue(entry, out var file))
        {
            file = new MappedZipEntryFile(this, entry, _contentCaching);
            _fileEntryCache[entry] = file;
        }

        return file;
    }


    private readonly Dictionary<string, MappedZipEntryDirectoryContent> _directoryCache = new();

    private MappedZipEntryDirectoryContent GetOrCreateDirectoryByRelativePath(string relativePath)
    {
        if (!_directoryCache.TryGetValue(relativePath, out var dir))
        {
            dir = new MappedZipEntryDirectoryContent(this, relativePath);
            _directoryCache[relativePath] = dir;
        }

        return dir;
    }
    #endregion
}