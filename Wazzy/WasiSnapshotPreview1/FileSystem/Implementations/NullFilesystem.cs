using System.Security.Cryptography.X509Certificates;
using Wasmtime;
using Wazzy.Interop;

namespace Wazzy.WasiSnapshotPreview1.FileSystem.Implementations;

public class NullFilesystem
    : IWasiFileSystem
{
    public WasiError Write(Caller caller, FileDescriptor fd, ReadonlyBuffer<ReadonlyBuffer<byte>> iovs, ref uint nwrittenOutput)
    {
        // Check if it's stdout or stderr
        if (fd.Handle != 1 && fd.Handle != 2)
            return WasiError.EBADF;

        // Extract the message
        var iovecs = iovs.GetSpan(caller);
        var totalWritten = 0u;
        for (var i = 0; i < iovecs.Length; i++)
        {
            var span = iovecs[i].GetSpan(caller);
            totalWritten += (uint)span.Length;
        }

        // Done
        nwrittenOutput = totalWritten;
        return WasiError.SUCCESS;
    }

    public PrestatGetResult PrestatGet(Caller caller, FileDescriptor fd, ref Prestat result)
    {
        return PrestatGetResult.BadFileDescriptor;
    }

    public PrestatDirNameResult PrestatDirName(Caller caller, FileDescriptor fd, Span<byte> name)
    {
        return PrestatDirNameResult.BadFileDescriptor;
    }

    public PathOpenResult PathOpen(Caller caller, FileDescriptor fd, LookupFlags lookup, ReadOnlySpan<byte> path, OpenFlags openFlags, FileRights baseRights, FileRights inheritingRights, FdFlags fdFlags, ref FileDescriptor outputFd)
    {
        return PathOpenResult.BadFileDescriptor;
    }

    public CloseResult Close(Caller caller, FileDescriptor fd)
    {
        return CloseResult.BadFileDescriptor;
    }

    public ReadDirectoryResult ReadDirectory(Caller caller, FileDescriptor fd, Span<byte> buffer, long cookie, ref uint bufUsedOutput)
    {
        return ReadDirectoryResult.BadFileDescriptor;
    }

    public WasiError PathCreateDirectory(Caller caller, FileDescriptor fd, ReadOnlySpan<byte> path)
    {
        return WasiError.EBADF;
    }

    public WasiError PWrite(Caller caller, FileDescriptor fd, Buffer<Buffer<byte>> iovs, long offset, ref uint nread)
    {
        return WasiError.EBADF;
    }

    public StatResult StatGet(Caller caller, FileDescriptor fd, ref FileStat result)
    {
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

    public StatResult PathStatGet(Caller caller, FileDescriptor fd, LookupFlags lookup, ReadOnlySpan<byte> path, ref FileStat result)
    {
        return StatResult.BadFileDescriptor;
    }

    public ReadResult Read(Caller caller, FileDescriptor fd, Buffer<Buffer<byte>> iovs, ref uint nread)
    {
        return ReadResult.BadFileDescriptor;
    }

    public ReadResult PRead(Caller caller, FileDescriptor fd, Buffer<Buffer<byte>> iovs, long offset, ref uint nread)
    {
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

    public WasiError FdStatGet(Caller caller, FileDescriptor fd, ref FdStat pointer)
    {
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
}