using System.Runtime.CompilerServices;
using System.Text;
using Wazzy.WasiSnapshotPreview1.Clock;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Directories;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Files;

namespace Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Builder;

public class VirtualFileSystemBuilder
{
    private bool _closed;

    private Action<DirectoryBuilder>? _root;
    private IFile? _stdin;
    private IFile? _stdout;
    private IFile? _stderr;
    private IVFSClock? _clock;
    private bool _readonly;
    private readonly List<string> _preopens = [];

    private void ThrowClosed([CallerMemberName] string name = "")
    {
        if (_closed)
            throw new InvalidOperationException($"Cannot use `VirtualFileSystemBuilder.{name}` after calling VirtualFileSystemBuilder.{nameof(Build)}");
    }

    public VirtualFileSystem Build()
    {
        ThrowClosed();
        _closed = true;

        var clock = _clock ?? new RealtimeClock();
        var root = BuildRoot(_root, clock);

        return new VirtualFileSystem(
            _readonly,
            clock,
            _stdin ?? new ZeroFile(),
            _stdout ?? new ZeroFile(),
            _stderr ?? new ZeroFile(),
            root,
            _preopens
        );
    }

    private static IDirectory BuildRoot(Action<DirectoryBuilder>? fileTree, IVFSClock clock)
    {
        if (fileTree is not null)
        {
            var content = new List<(string, Func<VfsContext, IFilesystemEntry>)>();
            var builder = new DirectoryBuilder(string.Empty, content);
            fileTree(builder);

            var context = new VfsContext { Clock = clock };

            var root = new VirtualDirectoryContent(clock);
            foreach (var (name, ctor) in content)
            {
                var child = ctor(context);
                root.Add(Encoding.UTF8.GetBytes(name).AsSpan()[1..], child);
            }

            return root;
        }

        return new VirtualDirectoryContent(clock);
    }

    public VirtualFileSystemBuilder WithVirtualRoot(Action<DirectoryBuilder> fileTree)
    {
        ThrowClosed();
        _root = fileTree;
        return this;
    }

    public VirtualFileSystemBuilder WithPipes(IFile? stdin = null, IFile? stdout = null, IFile? stderr = null)
    {
        ThrowClosed();

        if (stdin is { IsReadable: false })
            throw new ArgumentException("stdin must be readable");
        if (stdout is { IsWritable: false })
            throw new ArgumentException("stdout must be writeable");
        if (stderr is { IsWritable: false })
            throw new ArgumentException("stderr must be writeable");

        _stdin = stdin;
        _stdout = stdout;
        _stderr = stderr;
        return this;
    }

    public VirtualFileSystemBuilder WithClock(IVFSClock? clock)
    {
        ThrowClosed();
        _clock = clock;
        return this;
    }

    public VirtualFileSystemBuilder Readonly(bool isReadonly)
    {
        ThrowClosed();
        _readonly = isReadonly;
        return this;
    }

    internal VirtualFileSystemBuilder WithPreopen(string path)
    {
        _preopens.Add(path);
        return this;
    }
}