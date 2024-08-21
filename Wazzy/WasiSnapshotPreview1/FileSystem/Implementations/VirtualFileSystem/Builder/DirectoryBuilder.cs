using System.Text;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Directories;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Files;

namespace Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Builder;

public struct VfsContext
{
    public IVFSClock Clock { get; init; }
}

public class DirectoryBuilder
{
    private readonly string _fullPath;
    private readonly List<(string, Func<VfsContext, IFilesystemEntry>)> _contentConstructors;

    internal DirectoryBuilder(string fullPath, List<(string, Func<VfsContext, IFilesystemEntry>)> contentConstructors)
    {
        _fullPath = fullPath;
        _contentConstructors = contentConstructors;
    }

    public IDirectory Build(VfsContext context)
    {
        var root = new VirtualDirectoryContent(context.Clock);

        foreach (var (name, ctor) in _contentConstructors)
        {
            var child = ctor(context);
            root.Add(Encoding.UTF8.GetBytes(name), child);
        }

        _contentConstructors.Clear();

        return root;
    }

    private string ValidatePath(string path)
    {
        var changed = false;
        Span<byte> characters = stackalloc byte[path.Length * 2];
        var length = Encoding.UTF8.GetBytes(path, characters);
        characters = characters[..length];
        var utf8 = new PathUtf8(characters);

        // Don't allow indirect paths
        if (utf8.IsComplex())
            throw new ArgumentException("path must not contain '.' or '..'");

        // Don't allow absolute paths unless we are at the root
        if (characters[0] == (byte)'/')
        {
            if (_fullPath != "/")
                throw new ArgumentException("path must be relative");

            // Trim the leading /
            characters = characters[1..];
            changed = true;
        }

        // Trim trailing /
        if (characters[^1] == (byte)'/')
        {
            characters = characters[..^1];
            changed = true;
        }

        if (changed)
            return Encoding.UTF8.GetString(characters);

        return path;
    }

    public DirectoryBuilder CreateVirtualDirectory(string name, Action<DirectoryBuilder> content)
    {
        var path = _fullPath + '/' + ValidatePath(name);

        _contentConstructors.Add(
            (path, context => new VirtualDirectoryContent(context.Clock))
        );

        var builder = new DirectoryBuilder(path, _contentConstructors);
        content(builder);

        return this;
    }

    public DirectoryBuilder CreateVirtualDirectory(string name)
    {
        return CreateVirtualDirectory(name, _ => { });
    }

    public DirectoryBuilder CreateInMemoryFile(
        string name,
        ReadOnlyMemory<byte>? content = null,
        MemoryStream? backing = null,
        bool isReadOnly = false)
    {
        _contentConstructors.Add(
            (_fullPath + '/' + ValidatePath(name), context =>
            {
                var bytes = content != null ? content.Value.Span : [];
                var file = new InMemoryFile(context.Clock.GetTime(), bytes, backing) { IsWritable = !isReadOnly };
                return file;
            }
        )
        );

        return this;
    }

    public DirectoryBuilder MapFile(
        string name,
        string hostPath,
        bool isReadonly = false)
    {
        if (!File.Exists(hostPath))
            throw new ArgumentException($"file {hostPath} does not exist", nameof(hostPath));

        _contentConstructors.Add(
            (_fullPath + '/' + ValidatePath(name), context => new MappedFile(hostPath, context.Clock, isReadonly, true))
        );

        return this;
    }

    public DirectoryBuilder MapDirectory(
        string name,
        string hostPath,
        bool isReadOnly = false)
    {
        if (!Directory.Exists(hostPath))
            throw new ArgumentException($"directory {hostPath} does not exist", nameof(hostPath));

        var path = _fullPath + '/' + ValidatePath(name);

        _contentConstructors.Add(
            (path, context => new MappedDirectoryContent(hostPath, context.Clock, isReadOnly, true))
        );

        return this;
    }

    /// <summary>
    /// Map a zip archive into the filesystem as a read only directory.
    /// </summary>
    /// <remarks>
    /// The zip archive **must not** be modified externally while mapped into the virtual file system!
    /// </remarks>
    /// <param name="name"></param>
    /// <param name="hostPath"></param>
    /// <returns></returns>
    public DirectoryBuilder MapReadonlyZipArchiveDirectory(
        string name,
        string hostPath)
    {
        if (!File.Exists(hostPath))
            throw new ArgumentException($"File {hostPath} does not exist", nameof(hostPath));

        var path = _fullPath + '/' + ValidatePath(name);

        _contentConstructors.Add(
            (path, context => new MappedZipArchiveDirectoryContent(hostPath, context.Clock))
        );

        return this;
    }
}