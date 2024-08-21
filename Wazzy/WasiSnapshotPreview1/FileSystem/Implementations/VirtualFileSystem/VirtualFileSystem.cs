using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Wasmtime;
using Wazzy.Extensions;
using Wazzy.Interop;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Directories;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Files;

namespace Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem;

public sealed class VirtualFileSystem
    : IWasiFileSystem, IDisposable
{
    private static readonly ReadOnlyMemory<byte> RootName = ReadOnlyMemory<byte>.Empty;

    private readonly bool _readonly;
    private readonly IVFSClock _clock;

    private readonly IFile _stdin;
    private readonly IFile _stdout;
    private readonly IFile _stderr;

    private readonly IDirectory _root;
    private readonly List<string> _preopens;
    private readonly List<(FileDescriptor, ReadOnlyMemory<byte>)> _preOpened = [];
    private readonly Dictionary<FileDescriptor, IFilesystemHandle> _handles = [];

    private readonly object _globalLock = new();

    internal VirtualFileSystem(bool @readonly, IVFSClock clock, IFile stdin, IFile stdout, IFile stderr, IDirectory root, List<string> preopens)
    {
        _readonly = @readonly;
        _clock = clock;
        _stdin = stdin;
        _stdout = stdout;
        _stderr = stderr;
        _root = root;
        _preopens = preopens;

        // Create handles for default streams
        _handles.Add(new FileDescriptor(0), _stdin.Open(FdFlags.None));
        _handles.Add(new FileDescriptor(1), _stdout.Open(FdFlags.Append));
        _handles.Add(new FileDescriptor(2), _stderr.Open(FdFlags.Append));

        // Pre-open the root
        var descriptor = new FileDescriptor(3);
        var rootHandle = _root.Open();
        _handles.Add(descriptor, rootHandle);
        _preOpened.Add((descriptor, RootName));

        // Additional preopens
        var idx = 4;
        foreach (var pathStr in _preopens)
        {
            var fd = new FileDescriptor(idx++);
            var pathBytes = Encoding.UTF8.GetBytes(pathStr);
            var path = new PathUtf8(pathBytes);
            var hd = ((IDirectory)ResolvePath(path, _root)!).Open();
            _handles.Add(fd, hd);
            _preOpened.Add((fd, pathBytes));
        }
    }

    public void Dispose()
    {
        foreach (var handle in _handles)
            handle.Value.Dispose();
    }

    private FileDescriptor? AllocateFd()
    {
        // Seed an RNG with strong randomness
        var rng = new System.Random(RandomNumberGenerator.GetInt32(0, int.MaxValue));

        // Try to generate some FDs, if we can't find an open one after 1024 samples give up
        for (var i = 0; i < 1024; i++)
        {
            var fd = new FileDescriptor(rng.Next(110, int.MaxValue));
            if (!_handles.ContainsKey(fd))
                return fd;
        }

        return null;
    }

    private IFilesystemHandle? GetHandle(FileDescriptor fd)
    {
        _handles.TryGetValue(fd, out var handle);
        return handle;
    }

    private WasiError GetDirectory(FileDescriptor fd, out IDirectoryHandle? directory)
    {
        directory = null;

        var handle = GetHandle(fd);
        if (handle == null)
            return WasiError.ENOENT;

        directory = handle as IDirectoryHandle;
        if (directory == null)
            return WasiError.ENOTDIR;

        return WasiError.SUCCESS;
    }

    private WasiError GetFile(FileDescriptor fd, out IFileHandle? file)
    {
        file = null;

        var handle = GetHandle(fd);
        if (handle == null)
            return WasiError.ENOENT;

        file = handle as IFileHandle;
        if (file == null)
            return WasiError.EISDIR;

        return WasiError.SUCCESS;
    }

    private bool CloseFd(FileDescriptor fd)
    {
        if (_handles.TryGetValue(fd, out var handle))
        {
            handle.Dispose();
            _handles.Remove(fd);
            return true;
        }

        return false;
    }

    private ulong GetTimestamp()
    {
        return _clock.GetTime();
    }

    /// <summary>
    /// Given a path, get the item it points to
    /// </summary>
    /// <param name="path"></param>
    /// <param name="rootDirectory"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    private IFilesystemEntry? ResolvePath(PathUtf8 path, IDirectory? rootDirectory)
    {
        if (path.Length == 0)
            return null;
        if (path.Length == 1 && path.Bytes[0] == (byte)'.')
            return rootDirectory;

        // Resolve non-trivial paths
        if (path.IsComplex())
        {
            var segments = new List<byte[]>();
            var totalLength = 0;
            var remaining = path;
            do
            {
                remaining.Split(out var segment, out remaining);

                // A dot means `this directory`, don't move anywhere
                if (segment.Length == 1 && segment.Bytes[0] == (byte)'.')
                    continue;

                // A `..` means `parent directory`, remove the previous segment
                if (segment.Length == 2
                 && segment.Bytes[0] == (byte)'.'
                 && segment.Bytes[1] == (byte)'.')
                {
                    if (segments.Count > 0)
                    {
                        totalLength -= segments[segment.Length - 1].Length;
                        segments.RemoveAt(segments.Count - 1);
                    }
                }

                segments.Add(segment.Bytes.ToArray());
                totalLength += segment.Bytes.Length;
            } while (remaining.Length > 0);

            totalLength += segments.Count - 1;

            // Build the new path string
            var resolvedPathBytes = new byte[totalLength];
            var len = 0;
            for (var i = 0; i < segments.Count; i++)
            {
                for (var j = 0; j < segments[i].Length; j++)
                {
                    resolvedPathBytes[len] = segments[i][j];
                    len++;
                }

                if (i < segments.Count - 1)
                {
                    resolvedPathBytes[len] = (byte)'/';
                    len++;
                }
            }

            path = new PathUtf8(resolvedPathBytes);
        }

        if (path.IsAbsolute())
        {
            // Strip off the leading '/'
            var pathBytes = path.Bytes[1..];

            if (pathBytes.Length > 0)
                return _root.GetChild(pathBytes)?.Content;

            return _root;
        }

        // todo this should probably be communicated back to the caller via a wasi error
        ArgumentNullException.ThrowIfNull(rootDirectory, nameof(rootDirectory));

        return rootDirectory.GetChild(path.Bytes)?.Content;
    }

    /// <summary>
    /// Given a path to something, get the diectory that thing is in
    /// </summary>
    /// <param name="path"></param>
    /// <param name="rootDirectory"></param>
    /// <returns></returns>
    private IDirectory? ResolveParent(PathUtf8 path, IDirectory? rootDirectory)
    {
        path.Bytes.SplitLast((byte)'/', out var parentPath, out var name);

        if (name.Length == 0)
            return rootDirectory;

        return ResolvePath(new PathUtf8(parentPath), rootDirectory) as IDirectory;
    }

    private int? FindPreopenedFileHandleIndex(FileDescriptor fd)
    {
        for (var i = 0; i < _preOpened.Count; i++)
            if (_preOpened[i].Item1.Handle == fd.Handle)
                return i;

        return null;
    }

    PrestatGetResult IWasiFileSystem.PrestatGet(Caller caller, FileDescriptor fd, ref Prestat result)
    {
        lock (_globalLock)
        {
            // First 3 handles are stdin,out,err. 
            if (fd.Handle <= 2)
                return PrestatGetResult.BadFileDescriptor;

            var idx = FindPreopenedFileHandleIndex(fd);
            if (idx is null or < 0)
                return PrestatGetResult.BadFileDescriptor;

            result = new Prestat(PreopenType.Directory, _preOpened[idx.Value].Item2.Length);
            return PrestatGetResult.Success;
        }
    }

    PrestatDirNameResult IWasiFileSystem.PrestatDirName(Caller caller, FileDescriptor fd, Span<byte> name)
    {
        lock (_globalLock)
        {
            // First 3 handles are stdin,out,err.
            if (fd.Handle <= 2)
                return PrestatDirNameResult.BadFileDescriptor;

            var idx = FindPreopenedFileHandleIndex(fd);
            if (idx is null or < 0)
                return PrestatDirNameResult.BadFileDescriptor;

            // Check that the output buffer is exactly the right size
            var (_, preOpenedName) = _preOpened[idx.Value];
            if (preOpenedName.Length != name.Length)
                return PrestatDirNameResult.WrongBufferSize;

            // Write the name to the output buffer
            preOpenedName.Span.CopyTo(name);
            return PrestatDirNameResult.Success;
        }
    }

    WasiError IWasiFileSystem.ReadLinkAt(Caller caller, FileDescriptor fd, ReadOnlySpan<byte> path, Span<byte> result, ref int nwritten)
    {
        lock (_globalLock)
        {
            // Symbolic links are not supported, so therefore this one doesn't exist!
            return WasiError.ENOENT;
        }
    }

    WasiError IWasiFileSystem.PathSymLink(Caller caller, ReadOnlySpan<byte> oldPath, FileDescriptor fileDescriptor, ReadOnlySpan<byte> newPath)
    {
        lock (_globalLock)
        {
            // Symbolic links are not supported
            return WasiError.ENOTSUP;
        }
    }

    public WasiError FdRenumber(Caller caller, FileDescriptor from, FileDescriptor to)
    {
        return WasiError.ENOTSUP;
    }

    PathOpenResult IWasiFileSystem.PathOpen(
        Caller caller,
        FileDescriptor fd,
        LookupFlags lookup,
        ReadOnlySpan<byte> path,
        OpenFlags openFlags,
        FileRights _,
        FileRights __,
        FdFlags fdFlags,
        ref FileDescriptor outputFd
    )
    {
        lock (_globalLock)
        {
            return PathOpen(caller, fd, lookup, path, openFlags, fdFlags, ref outputFd);
        }
    }

    private PathOpenResult PathOpen(
        Caller caller,
        FileDescriptor fd,
        LookupFlags lookup,
        ReadOnlySpan<byte> path,
        OpenFlags openFlags,
        FdFlags fdFlags,
        ref FileDescriptor outputFd
    )
    {
        // Handles are effectively 31 bits, so the sign bit must be unset
        if (fd.Handle < 0)
            return PathOpenResult.BadFileDescriptor;

        // Cannot open one of stdin/stdout/stderr
        if (fd.Handle < 3)
            return PathOpenResult.BadFileDescriptor;

        var handle = GetHandle(fd);
        if (handle == null)
            return PathOpenResult.BadFileDescriptor;

        if (handle.FileType != FileType.Directory || handle is not IDirectoryHandle directory)
            return PathOpenResult.NotADirectory;

        // Check if we're trying to re-open a directory
        if (openFlags == OpenFlags.Directory && path.Length == 1 && path[0] == '.')
        {
            var newFd = AllocateFd();
            if (newFd == null)
                return PathOpenResult.NoFileDescriptorsAvailable;

            _handles.Add(newFd.Value, handle);
            outputFd = newFd.Value;
            return PathOpenResult.Success;
        }

        // Try to resolve the path
        var path8 = new PathUtf8(path);
        var dest = ResolvePath(path8, directory.Directory);

        // Check if the file does not exist
        if (dest == null)
        {
            if ((openFlags & OpenFlags.Create) == OpenFlags.Create)
                return PathOpenCreate(caller, directory, path8, openFlags, fdFlags, ref outputFd);

            return PathOpenResult.NoEntity;
        }

        if ((openFlags & OpenFlags.Exclusive) == OpenFlags.Exclusive)
            return PathOpenResult.AlreadyExists;

        switch (dest)
        {
            case IDirectory dir:
            {
                var newFd = AllocateFd();
                if (newFd == null)
                    return PathOpenResult.NoFileDescriptorsAvailable;

                _handles.Add(newFd.Value, dir.Open());
                outputFd = newFd.Value;
                return PathOpenResult.Success;
            }

            case IFile file:
            {
                // Cannot open a file in directory mode
                if ((openFlags & OpenFlags.Directory) == OpenFlags.Directory)
                    return PathOpenResult.NotADirectory;

                if ((openFlags & OpenFlags.Truncate) == OpenFlags.Truncate)
                {
                    if (!file.IsWritable)
                        return PathOpenResult.ReadOnly;
                    using var fileHandle = file.Open(fdFlags);
                    fileHandle.Truncate(GetTimestamp());
                    fileHandle.Seek(0, Whence.End, out _);
                }

                var newFd = AllocateFd();
                if (newFd == null)
                    return PathOpenResult.NoFileDescriptorsAvailable;

                _handles.Add(newFd.Value, file.Open(fdFlags));
                outputFd = newFd.Value;
                return PathOpenResult.Success;
            }
        }

        // Unknown VFS item type!
        return PathOpenResult.BadFileDescriptor;
    }

    private PathOpenResult PathOpenCreate(Caller caller, IDirectoryHandle root, PathUtf8 path, OpenFlags openFlags, FdFlags fdFlags, ref FileDescriptor outputFd)
    {
        if (_readonly)
            return PathOpenResult.ReadOnly;

        // Get the directory we want to create in
        var parentDir = ResolveParent(path, root.Directory);
        if (parentDir is null)
            return PathOpenResult.NoEntity;

        // Get the name of the thing we want to create
        var name = path.GetName();

        // Check if an item already exists with this name
        var item = parentDir.GetChild(name);
        if (item.HasValue)
        {
            // A directory already exists at this path
            if (item.Value.Content is IDirectory)
                return PathOpenResult.IsADirectory;

            // All filesystem content must be either a directory or a file!
            if (item.Value.Content is not IFile file)
            {
                //todo: Console.Error.WriteLine($"Unknown VirtualFileSystem item type: {item.Value.Content.GetType().Name}");

                return PathOpenResult.InvalidParameter;
            }

            // Fail if it was requested in exclusive mode
            if ((openFlags & OpenFlags.Exclusive) == OpenFlags.Exclusive)
                return PathOpenResult.AlreadyExists;

            // Not exclusive, so return a handle for that file
            var newFd = AllocateFd();
            if (newFd == null)
                return PathOpenResult.NoFileDescriptorsAvailable;

            _handles.Add(newFd.Value, file.Open(fdFlags));
            outputFd = newFd.Value;
        }
        else
        {
            // Allocate an FD for the new file
            var newFd = AllocateFd();
            if (newFd == null)
                return PathOpenResult.NoFileDescriptorsAvailable;

            // Create new file
            var (file, err) = parentDir.CreateFile(name);
            if (file == null)
                return err;

            _handles.Add(newFd.Value, ((IFile)file.Value.Content).Open(fdFlags));
            outputFd = newFd.Value;
        }

        return PathOpenResult.Success;
    }

    CloseResult IWasiFileSystem.Close(Caller caller, FileDescriptor fd)
    {
        lock (_globalLock)
        {
            // Cannot close StdIn, StdOut or StdErr
            if (fd.Handle < 3)
                return CloseResult.BadFileDescriptor;

            // If it is preopened, remove it from the list
            var preopenIdx = FindPreopenedFileHandleIndex(fd);
            if (preopenIdx is >= 0)
                _preOpened.RemoveAt(preopenIdx.Value);

            return CloseFd(fd)
                ? CloseResult.Success
                : CloseResult.BadFileDescriptor;
        }
    }

    ReadDirectoryResult IWasiFileSystem.ReadDirectory(Caller caller, FileDescriptor fd, Span<byte> outputBuffer, long cookie, ref uint bufUsedOutput)
    {
        lock (_globalLock)
        {
            var handle = GetHandle(fd);
            if (handle == null)
                return ReadDirectoryResult.BadFileDescriptor;

            if (handle.FileType != FileType.Directory || handle is not IDirectoryHandle directory)
                return ReadDirectoryResult.NotADirectory;

            if (cookie < 0)
                return ReadDirectoryResult.InvalidParameter;

            // If the cookie points past the end of the list of items then early exit and indicate success (all items have been enumerated)
            var items = directory.EnumerateChildren(GetTimestamp());
            if (cookie >= items.Count)
            {
                bufUsedOutput = 0;
                return ReadDirectoryResult.Success;
            }

            // Enumerate all items past the cookie and write them to the buffer
            var totalWritten = 0u;
            for (var i = (uint)cookie; i < items.Count; i++)
            {
                // Write this item to the buffer and note if the end of the buffer was reached
                var endOfBuffer = TryWrite(items[(int)i], i, outputBuffer, out var written);
                totalWritten += written;

                // If the end was reached we can't write any more
                if (endOfBuffer)
                    break;

                // Advanced the span to where the next item should be written
                outputBuffer = outputBuffer[written..];
            }

            bufUsedOutput = totalWritten;
            return ReadDirectoryResult.Success;

            static bool TryWrite(DirectoryItem item, uint index, Span<byte> dest, out ushort writtenBytes)
            {
                Span<byte> bytes = stackalloc byte[Marshal.SizeOf<DirEnt>() + item.NameUtf8.Length];

                // Write the directory entity to the start of the buffer
                var de = new DirEnt
                {
                    Next = index + 1,
                    INode = 0,
                    NameLength = (uint)item.NameUtf8.Length,
                    Type = item.Content.FileType
                };
                MemoryMarshal.Write(bytes, in de);

                // Write the UTF8 bytes of the name after the directory entity
                item.NameUtf8.Span.CopyTo(bytes[Marshal.SizeOf<DirEnt>()..]);

                // If the output span is smaller than the data we want to write then write as much as possible
                if (dest.Length < bytes.Length)
                {
                    bytes[..dest.Length].CopyTo(dest);
                    writtenBytes = (ushort)dest.Length;
                    return true;
                }

                // output span has enough space, write everything
                bytes.CopyTo(dest);
                writtenBytes = (ushort)bytes.Length;
                return false;
            }
        }
    }

    WasiError IWasiFileSystem.PathLink(Caller caller, FileDescriptor sourceRootFd, ReadOnlySpan<byte> sourcePath, int lookupFlags, FileDescriptor destRootFd, ReadOnlySpan<byte> destPath)
    {
        lock (_globalLock)
        {
            return WasiError.ENOTSUP;
        }
    }

    WasiError IWasiFileSystem.PathCreateDirectory(Caller caller, FileDescriptor fd, ReadOnlySpan<byte> pathBuffer)
    {
        lock (_globalLock)
        {
            if (_readonly)
                return WasiError.EROFS;

            var err = GetDirectory(fd, out var rootDir);
            if (err != WasiError.SUCCESS)
                return err;

            var pathSpan = new PathUtf8(pathBuffer);

            // Get the directory we want to create in
            var parent = ResolveParent(pathSpan, rootDir?.Directory);
            if (parent is null)
                return WasiError.ENOENT;

            // Get the name of the thing we want to create
            var name = pathSpan.GetName();

            // Create it
            return parent.CreateDirectory(name.ToArray()).Item2;
        }
    }

    WasiError IWasiFileSystem.Write(Caller caller, FileDescriptor fd, ReadonlyBuffer<ReadonlyBuffer<byte>> iovs, ref uint nwrittenOutput)
    {
        lock (_globalLock)
        {
            var handle = GetHandle(fd);
            if (handle == null)
                return WasiError.EBADF;

            if (handle.FileType == FileType.Directory || handle is not IFileHandle fileHandle)
                return WasiError.EISDIR;

            if (!fileHandle.File.IsWritable)
            {
                nwrittenOutput = 0;
                return WasiError.SUCCESS;
            }

            nwrittenOutput = fileHandle.Write(caller, iovs, GetTimestamp());

            return WasiError.SUCCESS;
        }
    }

    WasiError IWasiFileSystem.PWrite(Caller caller, FileDescriptor fd, ReadonlyBuffer<ReadonlyBuffer<byte>> iovs, long offset, ref uint nwrittenOutput)
    {
        lock (_globalLock)
        {
            var handle = GetHandle(fd);
            if (handle == null)
                return WasiError.EBADF;

            if (handle.FileType == FileType.Directory || handle is not IFileHandle fileHandle)
                return WasiError.EISDIR;

            if (!fileHandle.File.IsWritable)
            {
                nwrittenOutput = 0;
                return WasiError.SUCCESS;
            }

            // Save the current position
            var saveOffset = fileHandle.Position;

            // Seek to the offset
            var seekResult = fileHandle.Seek(offset, Whence.Set, out _);
            if (seekResult != SeekResult.Success)
                return (WasiError)seekResult;

            // Write the data
            nwrittenOutput = fileHandle.Write(caller, iovs, GetTimestamp());

            // Return to the saved offset
            fileHandle.Seek((long)saveOffset, Whence.Set, out _);

            return WasiError.SUCCESS;
        }
    }

    WasiError IWasiFileSystem.FdAdvise(Caller caller, FileDescriptor fd, long offset, long filesize, Advice advice)
    {
        lock (_globalLock)
        {
            // Check that the handle exists
            var handle = GetHandle(fd);
            if (handle == null)
                return WasiError.EBADF;

            // We don't actually do anything with the advice! Just return immediately.
            return WasiError.SUCCESS;
        }
    }

    WasiError IWasiFileSystem.FdAllocate(Caller caller, FileDescriptor fd, long offset, long length)
    {
        lock (_globalLock)
        {
            // Check that the handle exists
            var handle = GetHandle(fd);
            if (handle == null)
                return WasiError.EBADF;

            // We don't actually support this.
            return WasiError.ENOTSUP;
        }
    }

    WasiError IWasiFileSystem.StatSetSize(Caller caller, FileDescriptor fd, long size)
    {
        lock (_globalLock)
        {
            var handle = GetHandle(fd);
            if (handle == null)
                return WasiError.EBADF;

            if (handle.FileType == FileType.Directory || handle is not IFileHandle fileHandle)
                return WasiError.EISDIR;

            if (!fileHandle.File.IsWritable)
                return WasiError.EACCES;

            fileHandle.Truncate(_clock.GetTime(), size);
            return WasiError.SUCCESS;
        }
    }

    WasiError IWasiFileSystem.FdStatGet(Caller caller, FileDescriptor fd, ref FdStat statPtr)
    {
        lock (_globalLock)
        {
            var handle = GetHandle(fd);
            if (handle == null)
                return WasiError.EBADF;

            statPtr = handle.GetStat();

            return WasiError.SUCCESS;
        }
    }

    WasiError IWasiFileSystem.FdStatSetFlags(Caller caller, FileDescriptor fd, FdFlags flags)
    {
        lock (_globalLock)
        {
            var handle = GetHandle(fd);
            if (handle == null)
                return WasiError.EBADF;

            return handle.SetFlags(flags);
        }
    }

    WasiError IWasiFileSystem.FdStatSetTimes(Caller caller, FileDescriptor fd, long atime, long mtime, FstFlags fstFlags)
    {
        lock (_globalLock)
        {
            var handle = GetHandle(fd);
            if (handle == null)
                return WasiError.EBADF;

            return handle.SetTimes(_clock.GetTime(), atime, mtime, fstFlags);
        }
    }

    WasiError IWasiFileSystem.PathFileStatSetTimes(Caller caller, FileDescriptor fd, LookupFlags lookup, ReadOnlySpan<byte> path, long atime, long mtime, FstFlags fstFlags)
    {
        lock (_globalLock)
        {
            if (_readonly)
                return WasiError.EROFS;

            if (GetHandle(fd) is not IDirectoryHandle rootDir)
                return WasiError.EBADF;

            var item = ResolvePath(new PathUtf8(path), rootDir.Directory);
            if (item == null)
                return WasiError.ENOENT;

            return item.SetTimes(_clock.GetTime(), atime, mtime, fstFlags);
        }
    }

    StatResult IWasiFileSystem.StatGet(Caller caller, FileDescriptor fd, ref FileStat result)
    {
        lock (_globalLock)
        {
            var handle = GetHandle(fd);
            if (handle == null)
                return StatResult.BadFileDescriptor;

            result = handle.GetFileStat();
            return StatResult.Success;
        }
    }

    StatResult IWasiFileSystem.PathStatGet(Caller caller, FileDescriptor fd, LookupFlags lookup, ReadOnlySpan<byte> path, ref FileStat result)
    {
        lock (_globalLock)
        {
            FileDescriptor tmpFd = default;
            var openResult = PathOpen(caller, fd, lookup, path, OpenFlags.None, FdFlags.None, ref tmpFd);
            if (openResult != PathOpenResult.Success)
                return (StatResult)openResult;

            try
            {
                return ((IWasiFileSystem)this).StatGet(caller, tmpFd, ref result);
            }
            finally
            {
                ((IWasiFileSystem)this).Close(caller, tmpFd);
            }
        }
    }

    ReadResult IWasiFileSystem.Read(Caller caller, FileDescriptor fd, Buffer<Buffer<byte>> iovs, ref uint nreadPtr)
    {
        lock (_globalLock)
        {
            var handle = GetHandle(fd);
            if (handle == null)
                return ReadResult.BadFileDescriptor;

            if (handle.FileType == FileType.Directory || handle is not IFileHandle fileHandle)
                return ReadResult.IsDirectory;

            if (!fileHandle.File.IsReadable)
            {
                nreadPtr = 0;
                return ReadResult.Success;
            }

            var iovsSpan = iovs.GetSpan(caller);
            var nread = 0u;
            for (var i = 0; i < iovs.Length; i++)
            {
                var span = iovsSpan[i].GetSpan(caller);
                var read = fileHandle.Read(span, GetTimestamp());
                nread += read;

                if (read != span.Length)
                    break;
            }

            nreadPtr = nread;
            return ReadResult.Success;
        }
    }

    ReadResult IWasiFileSystem.PRead(Caller caller, FileDescriptor fd, Buffer<Buffer<byte>> iovs, long offset, ref uint nreadPtr)
    {
        lock (_globalLock)
        {
            var handle = GetHandle(fd);
            if (handle == null)
                return ReadResult.BadFileDescriptor;

            if (handle.FileType == FileType.Directory || handle is not IFileHandle fileHandle)
                return ReadResult.IsDirectory;

            if (!fileHandle.File.IsReadable)
            {
                nreadPtr = 0;
                return ReadResult.Success;
            }

            // Save the current file offset
            fileHandle.Seek(0, Whence.Current, out var saveOffset);

            // Seek to the new offset
            fileHandle.Seek(offset, Whence.Set, out _);

            var iovsSpan = iovs.GetSpan(caller);
            var nread = 0u;
            for (var i = 0; i < iovs.Length; i++)
            {
                var span = iovsSpan[i].GetSpan(caller);
                var read = fileHandle.Read(span, GetTimestamp());
                nread += read;

                if (read != span.Length)
                    break;
            }

            // Return to the saved offset
            fileHandle.Seek((long)saveOffset, Whence.Set, out _);

            nreadPtr = nread;
            return ReadResult.Success;
        }
    }

    SeekResult IWasiFileSystem.Seek(Caller caller, FileDescriptor fd, long offset, Whence whence, ref ulong newOffset)
    {
        lock (_globalLock)
        {
            var handle = GetHandle(fd);
            if (handle == null)
                return SeekResult.BadFileDescriptor;

            if (handle.FileType == FileType.Directory || handle is not IFileHandle fileHandle)
                return SeekResult.IsDirectory;

            return fileHandle.Seek(offset, whence, out newOffset);
        }
    }

    SyncResult IWasiFileSystem.Sync(Caller caller, FileDescriptor fd)
    {
        lock (_globalLock)
        {
            var handle = GetHandle(fd);
            if (handle == null)
                return SyncResult.BadFileDescriptor;

            try
            {
                if (handle is IFileHandle file)
                    file.Sync();
            }
            catch
            {
                return SyncResult.IoError;
            }

            return SyncResult.Success;
        }
    }

    WasiError IWasiFileSystem.PathRemoveDirectory(Caller caller, FileDescriptor fd, ReadOnlySpan<byte> pathBuffer)
    {
        lock (_globalLock)
        {
            if (_readonly)
                return WasiError.EROFS;

            var handle = GetHandle(fd);
            if (handle is not IDirectoryHandle rootDir)
                return WasiError.EBADF;

            var path = new PathUtf8(pathBuffer);
            var name = path.GetName();

            var parentDir = ResolveParent(path, rootDir.Directory);
            if (parentDir is null)
                return WasiError.ENOENT;

            var item = parentDir.GetChild(name);
            if (!item.HasValue)
                return WasiError.ENOENT;

            if (item.Value.Content is not IDirectory dir)
                return WasiError.ENOTDIR;

            using (var h = dir.Open())
            {
                if (h is IDirectoryHandle dirHandle && dirHandle.EnumerateChildren(GetTimestamp()).Count > 0)
                    return WasiError.ENOTEMPTY;
            }

            parentDir.Delete(name);
            return WasiError.SUCCESS;
        }
    }

    WasiError IWasiFileSystem.PathUnlinkFile(Caller caller, FileDescriptor fd, ReadOnlySpan<byte> pathBuffer)
    {
        lock (_globalLock)
        {
            if (_readonly)
                return WasiError.EROFS;

            var err = GetDirectory(fd, out var rootDir);
            if (err != WasiError.SUCCESS)
                return err;

            var path = new PathUtf8(pathBuffer);
            var name = path.GetName();

            var parentDir = ResolveParent(path, rootDir!.Directory);
            if (parentDir is null)
                return WasiError.ENOENT;

            var item = parentDir.GetChild(name);
            if (!item.HasValue)
                return WasiError.ENOENT;

            if (item.Value.Content is IDirectory)
                return WasiError.EISDIR;

            parentDir.Delete(name);
            return WasiError.SUCCESS;
        }
    }

    WasiError IWasiFileSystem.PathRename(Caller caller, FileDescriptor fd, ReadOnlySpan<byte> oldPathBuffer, FileDescriptor newFd, ReadOnlySpan<byte> newPathBuffer)
    {
        lock (_globalLock)
        {
            if (_readonly)
                return WasiError.EROFS;

            // Resolve handle
            var err1 = GetDirectory(fd, out var oldRootDir);
            if (err1 != WasiError.SUCCESS)
                return err1;
            var err2 = GetDirectory(newFd, out var newRootDir);
            if (err2 != WasiError.SUCCESS)
                return err1;

            // Get the directory relative to the root
            var oldPath = new PathUtf8(oldPathBuffer);
            var oldName = oldPath.GetName();
            var sourceDir = ResolveParent(oldPath, oldRootDir?.Directory);
            if (sourceDir is null)
                return WasiError.ENOENT;

            // Get the new directory
            var newPath = new PathUtf8(newPathBuffer);
            var destDir = ResolveParent(newPath, newRootDir?.Directory);
            if (destDir == null)
                return WasiError.ENOENT;

            return sourceDir.Move(oldName, destDir, newPath.GetName());
        }
    }

    public WasiError PollReadableBytes(FileDescriptor fd, out ulong readableBytes)
    {
        readableBytes = 0;

        lock (_globalLock)
        {
            var result = GetFile(fd, out var file);
            if (result != WasiError.SUCCESS)
                return result;

            readableBytes = file!.PollReadableBytes();
            return WasiError.SUCCESS;
        }
    }

    public WasiError PollWritableBytes(FileDescriptor fd, out ulong writableBytes)
    {
        writableBytes = 0;

        lock (_globalLock)
        {
            var result = GetFile(fd, out var file);
            if (result != WasiError.SUCCESS)
                return result;

            writableBytes = file!.PollWritableBytes();
            return WasiError.SUCCESS;
        }
    }
}