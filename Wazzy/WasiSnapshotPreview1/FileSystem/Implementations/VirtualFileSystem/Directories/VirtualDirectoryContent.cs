using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Files;

namespace Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Directories;

internal class VirtualDirectoryContent
        : IDirectory
{
    private class Handle
        : BaseDirectoryHandle
    {
        private readonly VirtualDirectoryContent _dir;
        public override IDirectory Directory => _dir;

        public Handle(VirtualDirectoryContent dir)
        {
            _dir = dir;
        }

        public override IReadOnlyList<DirectoryItem> EnumerateChildren(ulong timestamp)
        {
            _dir.AccessTime = timestamp;
            return _dir._children;
        }

        protected override ulong? TryGetChildCount()
        {
            return (ulong?)_dir._children.Count;
        }

        public override void Dispose()
        {
        }
    }

    private readonly List<DirectoryItem> _children = [ ];
    private readonly IVFSClock _clock;

    public bool CanMove => _children.All(c => c.Content.CanMove);
    public ulong AccessTime { get; set; }
    public ulong ModificationTime { get; set; }
    public ulong ChangeTime { get; set; }
    public FileType FileType => FileType.Directory;

    public VirtualDirectoryContent(IVFSClock clock)
    {
        _clock = clock;
        AccessTime = ModificationTime = ChangeTime = clock.GetTime();
    }

    public bool Add(ReadOnlySpan<byte> name, IFilesystemEntry child)
    {
        var nameUtf8 = new PathUtf8(name);
        if (nameUtf8.IsAbsolute())
            throw new InvalidOperationException($"Cannot add \"{nameUtf8.String}\" to folder; paths must be relative");

        nameUtf8.Split((byte)'/', out var folderName, out var rest);

        if (rest.Length > 0)
        {
            var folderContent = GetChild(folderName.Bytes);
            if (folderContent?.Content is not IDirectory folder)
            {
                folder = new VirtualDirectoryContent(_clock);
                if (!Add(folderName.Bytes, folder))
                    throw new InvalidOperationException($"{folderName.String} is not a directory");
            }

            return folder.Add(rest.Bytes, child);
        }

        var di = new DirectoryItem(name.ToArray(), child);
        if (GetChild(di.NameUtf8.Span) != null)
            return false;

        _children.Add(di);
        return true;
    }

    public DirectoryItem? GetChild(ReadOnlySpan<byte> relativePath)
    {
        new PathUtf8(relativePath).Split(out var first, out var rest);

        foreach (var item in _children)
        {
            if (item.NameUtf8.Span.SequenceEqual(first.Bytes))
            {
                if (rest.Length == 0)
                    return item;

                if (item.Content is IDirectory directory)
                    return directory.GetChild(rest.Bytes);

                return null;
            }
        }

        return null;
    }

    public (DirectoryItem?, PathOpenResult) Create(ReadOnlySpan<byte> name, IFilesystemEntry content)
    {
        if (GetChild(name) != null)
            return (null, PathOpenResult.AlreadyExists);

        var item = new DirectoryItem(name.ToArray(), content.ToInMemory());
        _children.Add(item);

        return (item, PathOpenResult.Success);
    }

    public (DirectoryItem?, PathOpenResult) CreateFile(ReadOnlySpan<byte> name, IFile? content = null)
    {
        return Create(name, content ?? new InMemoryFile(_clock.GetTime(), ReadOnlySpan<byte>.Empty));
    }

    public (DirectoryItem?, WasiError) CreateDirectory(ReadOnlySpan<byte> name, IDirectory? content = null)
    {
        var directory = content ?? new VirtualDirectoryContent(_clock);
        var (item, result) = Create(name, directory);
        return (item, (WasiError)result);
    }

    public bool Delete(ReadOnlySpan<byte> relativePath)
    {
        new PathUtf8(relativePath).Split(out var first, out var rest);

        for (var i = 0; i < _children.Count; i++)
        {
            if (_children[i].NameUtf8.Span.SequenceEqual(first.Bytes))
            {
                if (rest.Length == 0)
                {
                    _children.RemoveAt(i);
                    return true;
                }

                if (_children[i].Content is IDirectory directory)
                    return directory.Delete(rest.Bytes);

                return false;
            }
        }

        return false;
    }

    public WasiError Move(ReadOnlySpan<byte> currentName, IDirectory destinationDirectory, ReadOnlySpan<byte> destinationName)
    {
        new PathUtf8(currentName).Split(out var first, out var rest);

        for (var i = 0; i < _children.Count; i++)
        {
            if (_children[i].NameUtf8.Span.SequenceEqual(first.Bytes))
            {
                if (rest.Length == 0)
                {
                    var content = _children[i].Content;
                    if (!content.CanMove)
                    {
                        //todo:console.writeline!
                        Console.Error.WriteLine($"Cannot move '{new PathUtf8(currentName).String}', as mounted files cannot be moved");
                        return WasiError.EIO;
                    }

                    _children.RemoveAt(i);

                    var (_, result) = destinationDirectory.Create(destinationName, content);
                    return (WasiError)result;
                }

                if (_children[i].Content is IDirectory directory)
                    return directory.Move(rest.Bytes, destinationDirectory, destinationName);

                return (WasiError)PathOpenResult.NotADirectory;
            }
        }

        return (WasiError)PathOpenResult.NoEntity;
    }

    public IFilesystemHandle Open()
    {
        return new Handle(this);
    }

    public void Delete()
    {
    }

    public IFilesystemEntry ToInMemory()
    {
        return this;
    }
}