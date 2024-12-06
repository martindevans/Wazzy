using Wasmtime;
using Wazzy.Interop;

namespace Wazzy.WasiSnapshotPreview1.FileSystem;

public interface IWasiFileSystem
    : IWasiFeature
{
    /// <summary>
    /// The module which the exports of this feature are defined in
    /// </summary>
    public const string Module = "wasi_snapshot_preview1";

    /// <summary>
    /// Get the information associated with a pre-opened file descriptor. "Pre-opened" file descriptors are the roots of the file system and
    /// all other paths will be derived from the name of the preopened fds.
    ///
    /// Items 0,1 and 2 correspond to STDIN, STDOUT and STDERR. Preopened items come after that.
    /// </summary>
    /// <param name="caller">Context for this call</param>
    /// <param name="fd">File descriptor being probed.</param>
    /// <param name="result">Output information about this file descriptor.</param>
    /// <returns>Ok if the file descriptor is a known pre-opened fd, or else BadFileDescriptor</returns>
    public PrestatGetResult PrestatGet(Caller caller, FileDescriptor fd, out Prestat result);

    /// <summary>
    /// Get the name of a preopened file descriptor.
    /// </summary>
    /// <param name="caller">Context for this call</param>
    /// <param name="fd">File descriptor being queried</param>
    /// <param name="name">Output buffer for name, encoded as UTF8. Should be exactly the right size, return `WrongBufferSize` if not</param>
    /// <returns></returns>
    public PrestatDirNameResult PrestatDirName(Caller caller, FileDescriptor fd, Span<byte> name);

    /// <summary>
    /// Open a path relative to a file descriptor.
    /// </summary>
    /// <param name="caller">Context for this call</param>
    /// <param name="fd">Root file descriptor</param>
    /// <param name="lookup">Options controlling how to resolve the path</param>
    /// <param name="path">The path</param>
    /// <param name="openFlags">Controls how to open the final item in the path</param>
    /// <param name="baseRights"></param>
    /// <param name="inheritingRights"></param>
    /// <param name="fdFlags">Controls what flags to set on the new file descriptor</param>
    /// <param name="outputFd">The output file descriptor</param>
    /// <returns></returns>
    public PathOpenResult PathOpen(Caller caller, FileDescriptor fd, LookupFlags lookup, ReadOnlySpan<byte> path, OpenFlags openFlags, FileRights baseRights, FileRights inheritingRights, FdFlags fdFlags, out FileDescriptor outputFd);

    /// <summary>
    /// Close the given file descriptor (clean up all resources associated with it)
    /// </summary>
    /// <param name="caller">Context for this call</param>
    /// <param name="fd"></param>
    /// <returns></returns>
    public CloseResult Close(Caller caller, FileDescriptor fd);

    /// <summary>
    /// Read list of items from a directory.
    /// </summary>
    /// <param name="caller">Context for this call</param>
    /// <param name="fd">FileDescriptor of the directory to read</param>
    /// <param name="buffer">Output for resilts written as a sequence of `DirEnt` objects each followed by `DirEnt.NameLength` bytes
    /// holding the name. If there is insufficient space truncate the last entry</param>
    /// <param name="cookie">Indicates which directory entry to start with. Each `DirEnt` specifies the `cookie` of the next `DirEnt` in the sequence</param>
    /// <param name="bufUsed">The number of bytes used in `buffer`, if less than the size of the buffer that indicates that the end of the directory has been read</param>
    /// <returns></returns>
    public ReadDirectoryResult ReadDirectory(Caller caller, FileDescriptor fd, Span<byte> buffer, long cookie, out uint bufUsed);

    /// <summary>
    /// Create a directory at the given path, relative to the given fd
    /// </summary>
    /// <param name="caller">Context for this call</param>
    /// <param name="fd">Root directory which the path is relative to</param>
    /// <param name="path">Path of the new directory to create</param>
    /// <returns></returns>
    public WasiError PathCreateDirectory(Caller caller, FileDescriptor fd, ReadOnlySpan<byte> path);

    /// <summary>
    /// Write to a given file descriptor at the current offset
    /// </summary>
    /// <param name="caller">Context for this call</param>
    /// <param name="fd">File descriptor to write to</param>
    /// <param name="iovs">set of buffers to write</param>
    /// <param name="nwritten">total number of bytes written</param>
    /// <returns></returns>
    public WasiError Write(Caller caller, FileDescriptor fd, ReadonlyBuffer<ReadonlyBuffer<byte>> iovs, out uint nwritten);

    /// <summary>
    /// Write to a given file descriptor without updating the offset
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="fd"></param>
    /// <param name="iovs"></param>
    /// <param name="offset"></param>
    /// <param name="nwrittenOutput"></param>
    /// <returns></returns>
    public WasiError PWrite(Caller caller, FileDescriptor fd, ReadonlyBuffer<ReadonlyBuffer<byte>> iovs, long offset, out uint nwrittenOutput);

    /// <summary>
    /// Get a "FileStat" object for the given file descriptor
    /// </summary>
    /// <param name="caller">Context for this call</param>
    /// <param name="fd"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public StatResult StatGet(Caller caller, FileDescriptor fd, out FileStat result);

    /// <summary>
    /// Adjust the size of an open file. If this increases the file's size, the extra bytes are filled with zeros. Note: This is similar to ftruncate in POSIX.
    /// </summary>
    /// <param name="caller">Context for this call</param>
    /// <param name="fileDescriptor"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    public WasiError StatSetSize(Caller caller, FileDescriptor fileDescriptor, long size);

    public WasiError FdStatSetTimes(Caller caller, FileDescriptor fileDescriptor, long atime, long mtime, FstFlags fstFlags);

    /// <summary>
    /// Adjust the timestamps of a file or directory. Note: This is similar to `utimensat` in POSIX.
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="fileDescriptor"></param>
    /// <param name="lookup"></param>
    /// <param name="path"></param>
    /// <param name="atime"></param>
    /// <param name="mtime"></param>
    /// <param name="fstFlags"></param>
    /// <returns></returns>
    public WasiError PathFileStatSetTimes(Caller caller, FileDescriptor fileDescriptor, LookupFlags lookup, ReadOnlySpan<byte> path, long atime, long mtime, FstFlags fstFlags);

    /// <summary>
    /// Get a "FileStat" object for the object at the path relative to the given file descriptor
    /// </summary>
    /// <param name="caller">Context for this call</param>
    /// <param name="fd"></param>
    /// <param name="path"></param>
    /// <param name="result"></param>
    /// <param name="lookup"></param>
    /// <returns></returns>
    public StatResult PathStatGet(Caller caller, FileDescriptor fd, LookupFlags lookup, ReadOnlySpan<byte> path, out FileStat result);

    /// <summary>
    /// Read bytes from a file descriptor into a set of buffers
    /// </summary>
    /// <param name="caller">Context for this call</param>
    /// <param name="fd">File descriptor</param>
    /// <param name="iovs">Buffer of buffers to read data into sequentially</param>
    /// <param name="nread">Output for the total number of bytes read</param>
    /// <returns></returns>
    public ReadResult Read(Caller caller, FileDescriptor fd, Buffer<Buffer<byte>> iovs, Pointer<uint> nread);

    /// <summary>
    /// Read bytes from a file descriptor into a set of buffers, without using or updating the file offset
    /// </summary>
    /// <param name="caller">Context for this call</param>
    /// <param name="fd">File descriptor</param>
    /// <param name="iovs">Buffer of buffers to read data into sequentially</param>
    /// <param name="offset">Offset into the file</param>
    /// <param name="nread">Output for the total number of bytes read</param>
    /// <returns></returns>
    public ReadResult PRead(Caller caller, FileDescriptor fd, Buffer<Buffer<byte>> iovs, long offset, Pointer<uint> nread);

    /// <summary>
    /// Seek position to a new offset
    /// </summary>
    /// <param name="caller">Context for this call</param>
    /// <param name="fd"></param>
    /// <param name="offset"></param>
    /// <param name="whence"></param>
    /// <param name="newOffset"></param>
    /// <returns></returns>
    public SeekResult Seek(Caller caller, FileDescriptor fd, long offset, Whence whence, ref ulong newOffset);

    /// <summary>
    /// Get the current position in a file descriptor
    /// </summary>
    /// <param name="caller">Context for this call</param>
    /// <param name="fd"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public SeekResult Tell(Caller caller, FileDescriptor fd, ref ulong offset)
    {
        return Seek(caller, fd, 0, Whence.Current, ref offset);
    }

    /// <summary>
    /// Synchronize the data and metadata of a file to disk.
    /// </summary>
    /// <param name="caller">Context for this call</param>
    /// <param name="fd"></param>
    /// <returns></returns>
    public SyncResult Sync(Caller caller, FileDescriptor fd);

    /// <summary>
    /// Synchronize the data of a file to disk.
    /// </summary>
    /// <param name="caller">Context for this call</param>
    /// <param name="fd"></param>
    /// <returns></returns>
    public SyncResult DataSync(Caller caller, FileDescriptor fd)
    {
        return Sync(caller, fd);
    }

    public WasiError PathRemoveDirectory(Caller caller, FileDescriptor fd, ReadOnlySpan<byte> path);

    public WasiError PathUnlinkFile(Caller caller, FileDescriptor fd, ReadOnlySpan<byte> path);

    /// <summary>
    /// Rename a file or directory. This is similar to `renameat` in POSIX.
    /// </summary>
    /// <param name="caller">Context for this call</param>
    /// <param name="fd">File descriptor</param>
    /// <param name="oldPath">The working directory at which the resolution of the new path starts.</param>
    /// <param name="newFd">The working directory at which the resolution of the new path starts.</param>
    /// <param name="newPath">The destination path to which to rename the file or directory.</param>
    /// <returns></returns>
    public WasiError PathRename(Caller caller, FileDescriptor fd, ReadOnlySpan<byte> oldPath, FileDescriptor newFd, ReadOnlySpan<byte> newPath);

    /// <summary>
    /// Provide file advisory information on a file descriptor. This is similar to posix_fadvise in POSIX.
    /// </summary>
    /// <param name="caller">Context for this call</param>
    /// <param name="fd">File descriptor</param>
    /// <param name="offset">The offset within the file to which the advisory applies.</param>
    /// <param name="filesize">The length of the region to which the advisory applies.</param>
    /// <param name="advice">The advice.</param>
    /// <returns></returns>
    public WasiError FdAdvise(Caller caller, FileDescriptor fd, long offset, long filesize, Advice advice)
    {
        // First check the file actually exists by using filestat
        var statResult = StatGet(caller, fd, out _);
        if (statResult != StatResult.Success)
            return (WasiError)statResult;

        // Advice isn't important (whatever the advice, the result will ultimately be the same). Implement it
        // to just return success by default.
        return WasiError.SUCCESS;
    }

    /// <summary>
    /// Force the allocation of space in a file. This is similar to posix_fallocate in POSIX.
    /// </summary>
    /// <param name="caller">Context for this call</param>
    /// <param name="fd">File descriptor</param>
    /// <param name="offset">The offset within the file to allocate.</param>
    /// <param name="length">The length of the region to allocate.</param>
    /// <returns></returns>
    public WasiError FdAllocate(Caller caller, FileDescriptor fd, long offset, long length);

    public WasiError FdStatGet(Caller caller, FileDescriptor fd, out FdStat output);

    public WasiError FdStatSetFlags(Caller caller, FileDescriptor fd, FdFlags flags);

    public WasiError ReadLinkAt(Caller caller, FileDescriptor fd, ReadOnlySpan<byte> path, Span<byte> result, out int nwritten)
    {
        // Assume links are not supported, so therefore one doesn't exist
        nwritten = 0;
        return WasiError.ENOENT;
    }

    public WasiError PathLink(Caller caller, FileDescriptor sourceRootFd, ReadOnlySpan<byte> sourcePath, int lookupFlags, FileDescriptor destRootFd, ReadOnlySpan<byte> destPath)
    {
        // Assume hard links are not supported, so therefore one can't be created
        return WasiError.ENOTSUP;
    }

    public WasiError PathSymLink(Caller caller, ReadOnlySpan<byte> oldPath, FileDescriptor fileDescriptor, ReadOnlySpan<byte> newPath)
    {
        // Assume links are not supported, so therefore one can't be created
        return WasiError.ENOTSUP;
    }

    public WasiError FdRenumber(Caller caller, FileDescriptor from, FileDescriptor to)
    {
        // Assume renumbering is not supported
        return WasiError.ENOTSUP;
    }

    ///// <summary>
    ///// Check how many bytes can be read from the given file descriptor. This is used by poll_oneoff in IVirtualEventPoll.
    ///// </summary>
    ///// <param name="fd"></param>
    ///// <param name="readableBytes"></param>
    ///// <returns></returns>
    //public abstract WasiError PollReadableBytes(FileDescriptor fd, out ulong readableBytes);

    ///// <summary>
    ///// Check how many bytes can be written to the file descriptor. This is used by poll_oneoff in IVirtualEventPoll.
    ///// </summary>
    ///// <param name="fd"></param>
    ///// <param name="writableBytes"></param>
    ///// <returns></returns>
    //public abstract WasiError PollWritableBytes(FileDescriptor fd, out ulong writableBytes);

    void IWasiFeature.DefineOn(Linker linker)
    {
        linker.DefineFunction(Module, "fd_prestat_get",
                (Caller c, int fd, int ptr) => (int)PrestatGet(
                    c,
                    new FileDescriptor(fd),
                    out new Pointer<Prestat>(ptr).Deref(c)
                )
            );

        linker.DefineFunction(Module, "fd_prestat_dir_name",
            (Caller c, int fd, int buf, int len) => (int)PrestatDirName(
                c,
                new FileDescriptor(fd),
                new Buffer<byte>(buf, (uint)len).GetSpan(c)
            )
        );

        linker.DefineFunction(Module, "fd_filestat_get",
            (Caller c, int fd, int resultAddr) => (int)StatGet(
                c,
                new FileDescriptor(fd),
                out new Pointer<FileStat>(resultAddr).Deref(c)
            )
        );

        linker.DefineFunction(Module, "fd_filestat_set_size",
            (Caller c, int fd, long size) => (int)StatSetSize(
                c,
                new FileDescriptor(fd),
                size
            )
        );

        linker.DefineFunction(Module, "fd_filestat_set_times",
            (Caller c, int fd, long atime, long mtime, int fstFlags) => (int)FdStatSetTimes(
                c,
                new FileDescriptor(fd),
                atime,
                mtime,
                new FstFlags(fstFlags)
            )
        );

        linker.DefineFunction(Module, "path_filestat_set_times",
            (Caller c, int fd, int lookupFlags, int pathAddr, int pathLen, long atime, long mtime, int fstFlags) => (int)PathFileStatSetTimes(
                c,
                new FileDescriptor(fd),
                (LookupFlags)lookupFlags,
                new ReadonlyBuffer<byte>(pathAddr, (uint)pathLen).GetSpan(c),
                atime,
                mtime,
                new FstFlags(fstFlags)
            )
        );

        linker.DefineFunction(Module, "path_filestat_get",
            (Caller c, int fd, int lookupFlags, int pathAddr, int pathLen, int resultAddr) => (int)PathStatGet(
                c,
                new FileDescriptor(fd),
                (LookupFlags)lookupFlags,
                new ReadonlyBuffer<byte>(pathAddr, (uint)pathLen).GetSpan(c),
                out new Pointer<FileStat>(resultAddr).Deref(c)
            )
        );

        linker.DefineFunction(Module, "fd_fdstat_get",
            (Caller c, int fd, int resultAddr) => (int)FdStatGet(
                c,
                new FileDescriptor(fd),
                out new Pointer<FdStat>(resultAddr).Deref(c)
            )
        );

        linker.DefineFunction(Module, "fd_fdstat_set_flags",
            (Caller c, int fd, int flags) => (int)FdStatSetFlags(
                c,
                new FileDescriptor(fd),
                (FdFlags)flags
            )
        );

        linker.DefineFunction(Module, "fd_read",
            (Caller c, int fd, int iovsAddr, int iovsCount, int nreadAddr) => (int)Read(
                c,
                new FileDescriptor(fd),
                new Buffer<Buffer<byte>>(iovsAddr, (uint)iovsCount),
                new Pointer<uint>(nreadAddr)
            )
        );

        linker.DefineFunction(Module, "fd_pread",
            (Caller c, int fd, int iovsAddr, int iovsCount, long offset, int nreadAddr) => (int)PRead(
                c,
                new FileDescriptor(fd),
                new Buffer<Buffer<byte>>(iovsAddr, (uint)iovsCount),
                offset,
                new Pointer<uint>(nreadAddr)
            )
        );

        linker.DefineFunction(Module, "fd_seek",
            (Caller c, int fd, long offset, int whence, int newOffsetResult) => (int)Seek(
                c,
                new FileDescriptor(fd),
                offset,
                (Whence)whence,
                ref new Pointer<ulong>(newOffsetResult).Deref(c)
            )
        );

        linker.DefineFunction(Module, "fd_tell",
            (Caller c, int fd, int offsetResult) => (int)Tell(
                c,
                new FileDescriptor(fd),
                ref new Pointer<ulong>(offsetResult).Deref(c)
            )
        );

        linker.DefineFunction(Module, "path_open",
            (Caller c, int fd, int dirFlags, int pathPtr, int pathLen, int oFlagsInt, long fsRightsBase, long fsRightsInheriting, int fdFlagsInt, int fdPtr) => (int)PathOpen(
                c,
                new FileDescriptor(fd),
                (LookupFlags)dirFlags,
                new ReadonlyBuffer<byte>(pathPtr, (uint)pathLen).GetSpan(c),
                (OpenFlags)oFlagsInt,
                (FileRights)fsRightsBase,
                (FileRights)fsRightsInheriting,
                (FdFlags)fdFlagsInt,
                out new Pointer<FileDescriptor>(fdPtr).Deref(c)
            )
        );

        linker.DefineFunction(Module, "fd_advise",
            (Caller c, int fd, long offset, long size, int advice) => (int)FdAdvise(
                c,
                new FileDescriptor(fd),
                offset,
                size,
                (Advice)advice
            )
        );

        linker.DefineFunction(Module, "fd_allocate",
            (Caller c, int fd, long offset, long size) => (int)FdAllocate(
                c,
                new FileDescriptor(fd),
                offset,
                size
            )
        );

        linker.DefineFunction(Module, "fd_close",
            (Caller c, int fd) => (int)Close(
                c,
                new FileDescriptor(fd)
            )
        );

        linker.DefineFunction(Module, "fd_sync",
            (Caller c, int fd) => (int)Sync(
                c,
                new FileDescriptor(fd)
            )
        );

        linker.DefineFunction(Module, "fd_datasync",
            (Caller c, int fd) => (int)DataSync(
                c,
                new FileDescriptor(fd)
            )
        );

        linker.DefineFunction(Module, "fd_readdir",
            (Caller c, int fd, int dirEntPtr, int bufferLen, long cookie, int bufUsedPtr) => (int)ReadDirectory(
                c,
                new FileDescriptor(fd),
                new Buffer<byte>(dirEntPtr, (uint)bufferLen).GetSpan(c),
                cookie,
                out new Pointer<uint>(bufUsedPtr).Deref(c)
            )
        );

        linker.DefineFunction(Module, "path_create_directory",
            (Caller c, int fd, int pathPtr, int pathLen) => (int)PathCreateDirectory(
                c,
                new FileDescriptor(fd),
                new ReadonlyBuffer<byte>(pathPtr, (uint)pathLen).GetSpan(c)
            )
        );

        linker.DefineFunction(Module, "fd_write",
            (Caller c, int fd, int iovsAddr, int iovsCount, int nwrittenAddr) => (int)Write(
                c,
                new FileDescriptor(fd),
                new ReadonlyBuffer<ReadonlyBuffer<byte>>(iovsAddr, (uint)iovsCount),
                out new Pointer<uint>(nwrittenAddr).Deref(c)
            )
        );

        linker.DefineFunction(Module, "fd_pwrite",
            (Caller c, int fd, int iovsAddr, int iovsCount, long offset, int nwrittenAddr) => (int)PWrite(
                c,
                new FileDescriptor(fd),
                new ReadonlyBuffer<ReadonlyBuffer<byte>>(iovsAddr, (uint)iovsCount),
                offset,
                out new Pointer<uint>(nwrittenAddr).Deref(c)
            )
        );

        linker.DefineFunction(Module, "path_remove_directory",
            (Caller c, int fd, int pathPtr, int pathLen) => (int)PathRemoveDirectory(
                c,
                new FileDescriptor(fd),
                new ReadonlyBuffer<byte>(pathPtr, (uint)pathLen).GetSpan(c)
            )
        );

        linker.DefineFunction(Module, "path_unlink_file",
            (Caller c, int fd, int pathPtr, int pathLen) => (int)PathUnlinkFile(
                c,
                new FileDescriptor(fd),
                new ReadonlyBuffer<byte>(pathPtr, (uint)pathLen).GetSpan(c)
            )
        );

        linker.DefineFunction(Module, "path_rename",
            (Caller c, int fd, int oldPathPtr, int oldPathLen, int newFd, int newPathPtr, int newPathLen) => (int)PathRename(
                c,
                new FileDescriptor(fd),
                new ReadonlyBuffer<byte>(oldPathPtr, (uint)oldPathLen).GetSpan(c),
                new FileDescriptor(newFd),
                new ReadonlyBuffer<byte>(newPathPtr, (uint)newPathLen).GetSpan(c)
            )
        );

        linker.DefineFunction(Module, "path_readlink",
            (Caller c, int fd, int pathPtr, int pathLen, int outputPtr, int outputLen, int nwrittenPtr) => (int)ReadLinkAt(
                c,
                new FileDescriptor(fd),
                new ReadonlyBuffer<byte>(pathPtr, (uint)pathLen).GetSpan(c),
                new Buffer<byte>(outputPtr, (uint)outputLen).GetSpan(c),
                out new Pointer<int>(nwrittenPtr).Deref(c)
            )
        );

        linker.DefineFunction(Module, "path_link",
            (Caller c, int fd, int lookupFlags, int pathPtr, int pathLen, int newFd, int newPathPtr, int newPathLen) => (int)PathLink(
                c,
                new FileDescriptor(fd),
                new ReadonlyBuffer<byte>(pathPtr, (uint)pathLen).GetSpan(c),
                lookupFlags,
                new FileDescriptor(newFd),
                new ReadonlyBuffer<byte>(newPathPtr, (uint)newPathLen).GetSpan(c)
            )
        );

        linker.DefineFunction(Module, "path_symlink",
            (Caller c, int oldPathPtr, int oldPathLen, int fd, int newPathPtr, int newPathLen) => (int)PathSymLink(
                c,
                new ReadonlyBuffer<byte>(oldPathPtr, (uint)oldPathLen).GetSpan(c),
                new FileDescriptor(fd),
                new ReadonlyBuffer<byte>(newPathPtr, (uint)newPathLen).GetSpan(c)
            )
        );

        linker.DefineFunction(Module, "fd_renumber",
            (Caller c, int fd, int to) => (int)FdRenumber(
                c,
                new FileDescriptor(fd),
                new FileDescriptor(to)
            )
        );
    }
}