using Wasmtime;
using Wazzy.Interop;

namespace Wazzy.WasiSnapshotPreview1.FileSystem.Implementations;

/// <summary>
/// All FS operations fail, except for writing to stdout/stderr which succeed but do not print anywhere
/// </summary>
public class NullFilesystem
    : IWasiFileSystem
{
    public WasiError Write(Caller caller, FileDescriptor fd, ReadonlyBuffer<ReadonlyBuffer<byte>> iovs, out uint nwritten)
    {
        nwritten = 0;

        // Check if it's stdout or stderr
        if (fd.Handle != 1 && fd.Handle != 2)
            return WasiError.EBADF;

        // Extract the message
        var iovecs = iovs.GetSpan(caller);
        for (var i = 0; i < iovecs.Length; i++)
        {
            var span = iovecs[i].GetSpan(caller);
            nwritten += (uint)span.Length;
        }

        // Done
        return WasiError.SUCCESS;
    }

    public PrestatGetResult PrestatGet(Caller caller, FileDescriptor fd, out Prestat result)
    {
        result = default;
        return PrestatGetResult.BadFileDescriptor;
    }

    public PrestatDirNameResult PrestatDirName(Caller caller, FileDescriptor fd, Span<byte> name)
    {
        return PrestatDirNameResult.BadFileDescriptor;
    }

    public PathOpenResult PathOpen(Caller caller, FileDescriptor fd, LookupFlags lookup, ReadOnlySpan<byte> path, OpenFlags openFlags, FileRights baseRights, FileRights inheritingRights, FdFlags fdFlags, out FileDescriptor outputFd)
    {
        outputFd = default;
        return PathOpenResult.BadFileDescriptor;
    }

    public CloseResult Close(Caller caller, FileDescriptor fd)
    {
        return CloseResult.BadFileDescriptor;
    }

    public ReadDirectoryResult ReadDirectory(Caller caller, FileDescriptor fd, Span<byte> buffer, long cookie, out uint bufUsed)
    {
        bufUsed = 0;
        return ReadDirectoryResult.BadFileDescriptor;
    }

    public WasiError PathCreateDirectory(Caller caller, FileDescriptor fd, ReadOnlySpan<byte> path)
    {
        return WasiError.EBADF;
    }

    public WasiError PWrite(Caller caller, FileDescriptor fd, ReadonlyBuffer<ReadonlyBuffer<byte>> iovs, long offset, out uint nread)
    {
        nread = 0;
        return WasiError.EBADF;
    }

    public StatResult StatGet(Caller caller, FileDescriptor fd, out FileStat result)
    {
        result = default;
        return StatResult.BadFileDescriptor;
    }

    public WasiError StatSetSize(Caller caller, FileDescriptor fileDescriptor, long size)
    {
        return WasiError.EBADF;
    }

    public WasiError FdStatSetTimes(Caller caller, FileDescriptor fileDescriptor, long atime, long mtime, FstFlags fstFlags)
    {
        return WasiError.EBADF;
    }

    public WasiError PathFileStatSetTimes(Caller caller, FileDescriptor fileDescriptor, LookupFlags lookup, ReadOnlySpan<byte> path, long atime, long mtime, FstFlags fstFlags)
    {
        return WasiError.EBADF;
    }

    public StatResult PathStatGet(Caller caller, FileDescriptor fd, LookupFlags lookup, ReadOnlySpan<byte> path, out FileStat result)
    {
        result = default;
        return StatResult.BadFileDescriptor;
    }

    public ReadResult Read(Caller caller, FileDescriptor fd, Buffer<Buffer<byte>> iovs, out uint nread)
    {
        nread = 0;
        return ReadResult.BadFileDescriptor;
    }

    public ReadResult PRead(Caller caller, FileDescriptor fd, Buffer<Buffer<byte>> iovs, long offset, out uint nread)
    {
        nread = 0;
        return ReadResult.BadFileDescriptor;
    }

    public SeekResult Seek(Caller caller, FileDescriptor fd, long offset, Whence whence, ref ulong newOffset)
    {
        return SeekResult.BadFileDescriptor;
    }

    public SyncResult Sync(Caller caller, FileDescriptor fd)
    {
        return SyncResult.BadFileDescriptor;
    }

    public WasiError PathRemoveDirectory(Caller caller, FileDescriptor fd, ReadOnlySpan<byte> path)
    {
        return WasiError.EBADF;
    }

    public WasiError PathUnlinkFile(Caller caller, FileDescriptor fd, ReadOnlySpan<byte> path)
    {
        return WasiError.EBADF;
    }

    public WasiError PathRename(Caller caller, FileDescriptor fd, ReadOnlySpan<byte> oldPath, FileDescriptor newFd, ReadOnlySpan<byte> newPath)
    {
        return WasiError.EBADF;
    }

    public WasiError FdAdvise(Caller caller, FileDescriptor fd, long offset, long filesize, Advice advice)
    {
        return WasiError.EBADF;
    }

    public WasiError FdAllocate(Caller caller, FileDescriptor fd, long offset, long length)
    {
        return WasiError.EBADF;
    }

    public WasiError FdStatGet(Caller caller, FileDescriptor fd, out FdStat stat)
    {
        stat = default;
        return WasiError.EBADF;
    }

    public WasiError FdStatSetFlags(Caller caller, FileDescriptor fd, FdFlags flags)
    {
        return WasiError.EBADF;
    }

    public WasiError ReadLinkAt(Caller caller, FileDescriptor fd, ReadOnlySpan<byte> path, Span<byte> result, ref int nwritten)
    {
        return WasiError.EBADF;
    }

    public WasiError PathLink(Caller caller, FileDescriptor sourceRootFd, ReadOnlySpan<byte> sourcePath, int lookupFlags, FileDescriptor destRootFd, ReadOnlySpan<byte> destPath)
    {
        return WasiError.EBADF;
    }

    public WasiError PathSymLink(Caller caller, ReadOnlySpan<byte> oldPath, FileDescriptor fileDescriptor, ReadOnlySpan<byte> newPath)
    {
        return WasiError.EBADF;
    }

    public WasiError FdRenumber(Caller caller, FileDescriptor from, FileDescriptor to)
    {
        return WasiError.EBADF;
    }
}