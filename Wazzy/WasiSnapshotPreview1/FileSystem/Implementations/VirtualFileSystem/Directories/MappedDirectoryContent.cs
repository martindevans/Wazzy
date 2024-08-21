using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Files;

namespace Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Directories;

public class MappedDirectoryContent
        : IDirectory
{
    private class Handle
        : BaseDirectoryHandle
    {
        private readonly MappedDirectoryContent _dir;

        public Handle(MappedDirectoryContent dir)
        {
            _dir = dir;
        }

        public override void Dispose()
        {
        }

        public override IDirectory Directory => _dir;

        public override IReadOnlyList<DirectoryItem> EnumerateChildren(ulong timestamp)
        {
            var items = new List<DirectoryItem>();

            var children = _dir._info.EnumerateFileSystemInfos();
            foreach (var child in children)
            {
                IFilesystemEntry content;
                if ((child.Attributes & FileAttributes.Directory) != 0)
                    content = new MappedDirectoryContent(child.FullName, _dir._clock, _dir._isReadonly, false);
                else
                    content = new MappedFile(child.FullName, _dir._clock, _dir._isReadonly, false);

                items.Add(new DirectoryItem(child.Name, content));
            }

            return items;
        }

        protected override ulong? TryGetChildCount()
        {
            _dir._info.Refresh();
            return (ulong?)_dir._info.EnumerateFileSystemInfos().Count();
        }
    }

    private readonly IVFSClock _clock;
    private readonly bool _isReadonly;
    private readonly bool _isMountPoint;
    private readonly DirectoryInfo _info;

    public bool CanMove => !_isMountPoint;
    public FileType FileType => FileType.Directory;

    public ulong AccessTime
    {
        get => _clock.FromRealTime(new DateTimeOffset(_info.LastAccessTimeUtc));
        set => _info.LastAccessTimeUtc = _clock.ToRealTime(value).DateTime;
    }

    public ulong ModificationTime
    {
        get => _clock.FromRealTime(new DateTimeOffset(_info.LastWriteTimeUtc));
        set => _info.LastWriteTime = _clock.ToRealTime(value).DateTime;
    }

    public ulong ChangeTime
    {
        get => ModificationTime;
        set => ModificationTime = value;
    }

    public MappedDirectoryContent(string path, IVFSClock clock, bool isReadonly, bool isMountPoint)
    {
        if (!Directory.Exists(path))
            throw new FileNotFoundException(path);

        _clock = clock;
        _isReadonly = isReadonly;
        _isMountPoint = isMountPoint;
        _info = new DirectoryInfo(path);
    }

    private string GetFullPath(ReadOnlySpan<byte> relative)
    {
        return Path.Combine(_info.FullName, new PathUtf8(relative).String);
    }

    public void Delete()
    {
        if (CanMove)
            _info.Delete();
    }

    public bool Add(ReadOnlySpan<byte> name, IFilesystemEntry child)
    {
        // Not supported in mapped directories
        return false;
    }

    public DirectoryItem? GetChild(ReadOnlySpan<byte> relativePath)
    {
        // Construct a new mapped node to the host file system
        var fullPath = GetFullPath(relativePath);
        if (File.Exists(fullPath))
            return new DirectoryItem(Path.GetFileName(fullPath), new MappedFile(fullPath, _clock, _isReadonly, false));
        if (Directory.Exists(fullPath))
            return new DirectoryItem(Path.GetFileName(fullPath), new MappedDirectoryContent(fullPath, _clock, _isReadonly, false));

        return null;
    }

    public (DirectoryItem?, PathOpenResult) Create(ReadOnlySpan<byte> name, IFilesystemEntry content)
    {
        // Sanity check that we are not trying to move content which should not be moved
        if (!content.CanMove)
            throw new InvalidOperationException($"{content} cannot be moved");

        // Check to see if the content already exists
        var fullPath = GetFullPath(name);
        if (File.Exists(fullPath))
        {
            // Check to see if the file already exists in the host filesystem in the target location
            if (content is MappedFile mapped && mapped.HostPath == fullPath)
                return (new DirectoryItem(Path.GetFileName(fullPath), content), PathOpenResult.Success);

            return (null, PathOpenResult.AlreadyExists);
        }

        // Get or create the parent directory
        var parentDirectory = Path.GetDirectoryName(fullPath);
        if (parentDirectory != null && !Directory.Exists(parentDirectory))
            Directory.CreateDirectory(parentDirectory);

        IFilesystemEntry mappedContent;

        if (content is IDirectory directory)
        {
            // Recursively move directory content into the new location
            if (directory is MappedDirectoryContent source)
                source._info.MoveTo(fullPath);
            else
            {
                // Create new directory
                Directory.CreateDirectory(fullPath);

                // Copy children into new directory
                using var handle = (IDirectoryHandle)directory.Open();
                foreach (var child in handle.EnumerateChildren(_clock.GetTime()))
                {
                    var path = new byte[name.Length + child.NameUtf8.Length + 1];
                    name.CopyTo(path.AsSpan());
                    path[name.Length] = (byte)'/';
                    child.NameUtf8.CopyTo(path.AsMemory()[(name.Length + 1)..]);

                    Create(path, child.Content);
                }
            }

            mappedContent = new MappedDirectoryContent(fullPath, _clock, _isReadonly, false);
        }
        else
        {
            // Move file content into the new location
            switch (content)
            {
                case MappedFile mappedFile:
                    File.Move(mappedFile.HostPath, fullPath);
                    break;
                case IFile file:
                    {
                        using var stream = File.Open(fullPath, FileMode.CreateNew);
                        file.MoveTo(stream, _clock.GetTime());
                        break;
                    }
                default:
                    throw new ArgumentException("unrecognised file type", nameof(content));
            }

            mappedContent = new MappedFile(fullPath, _clock, _isReadonly, false);
        }

        return (new DirectoryItem(Path.GetFileName(fullPath), mappedContent), PathOpenResult.Success);
    }

    public (DirectoryItem?, PathOpenResult) CreateFile(ReadOnlySpan<byte> name, IFile? content = null)
    {
        if (content == null)
        {
            var path = GetFullPath(name);
            using (File.Create(path)) { }

            content = new MappedFile(path, _clock, _isReadonly, false);
        }

        return Create(name, content);
    }

    public (DirectoryItem?, WasiError) CreateDirectory(ReadOnlySpan<byte> name, IDirectory? content = null)
    {
        var (item, error) = Create(name, content ?? new VirtualDirectoryContent(_clock));
        return (item, (WasiError)error);
    }

    public IFilesystemHandle Open()
    {
        return new Handle(this);
    }

    public bool Delete(ReadOnlySpan<byte> name)
    {
        try
        {
            File.Delete(GetFullPath(name));
        }
        catch (IOException e)
        {
            //todo:console.writeline!
            Console.Error.WriteLine(e);
            return false;
        }

        return true;
    }

    public WasiError Move(ReadOnlySpan<byte> currentName, IDirectory destinationDirectory, ReadOnlySpan<byte> destinationName)
    {
        try
        {
            var fullPath = GetFullPath(currentName);
            if (File.Exists(fullPath))
            {
                if (destinationDirectory is MappedDirectoryContent hostDestinationDir)
                {
                    var destinationFullPath = hostDestinationDir.GetFullPath(destinationName);
                    File.Move(fullPath, destinationFullPath);
                }
                else
                {
                    var content = new MappedFile(fullPath, _clock, _isReadonly, false);
                    var (_, result) = destinationDirectory.Create(destinationName, content);
                    if (result == PathOpenResult.Success)
                        File.Delete(fullPath);

                    return (WasiError)result;
                }
            }
            else if (Directory.Exists(fullPath))
            {
                if (destinationDirectory is MappedDirectoryContent hostDestinationDir)
                {
                    var destinationFullPath = hostDestinationDir.GetFullPath(destinationName);
                    Directory.Move(fullPath, destinationFullPath);
                }
                else
                {
                    var content = new MappedDirectoryContent(fullPath, _clock, _isReadonly, false);
                    var (_, result) = destinationDirectory.Create(destinationName, content);
                    if (result == PathOpenResult.Success)
                        Directory.Delete(fullPath, true);

                    return (WasiError)result;
                }
            }
        }
        catch (IOException e)
        {
            //todo:console.writeline!
            Console.Error.WriteLine(e);
            return WasiError.EIO;
        }

        return WasiError.SUCCESS;
    }

    public IFilesystemEntry ToInMemory()
    {
        var inMemoryCopy = new VirtualDirectoryContent(_clock);

        using var handle = Open() as IDirectoryHandle;
        foreach (var child in handle!.EnumerateChildren(_clock.GetTime()))
        {
            var inMemoryChild = child.Content.ToInMemory();
            inMemoryCopy.Create(child.NameUtf8.Span, inMemoryChild);
        }

        return inMemoryCopy;
    }
}