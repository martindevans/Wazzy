using System.Security.Cryptography.X509Certificates;
using Wasmtime;
using Wazzy.Interop;

namespace Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem;

public class VirtualFileSystem
    : IWasiFileSystem
{
    public PrestatGetResult PrestatGet(Caller caller, FileDescriptor fd, ref Prestat result)
    {
        throw new NotImplementedException();
    }

    public PrestatDirNameResult PrestatDirName(Caller caller, FileDescriptor fd, Span<byte> name)
    {
        throw new NotImplementedException();
    }

    public PathOpenResult PathOpen(Caller caller, FileDescriptor fd, LookupFlags lookup, ReadOnlySpan<byte> path, OpenFlags openFlags, FileRights baseRights, FileRights inheritingRights, FdFlags fdFlags, ref FileDescriptor outputFd)
    {
        throw new NotImplementedException();
    }

    public CloseResult Close(Caller caller, FileDescriptor fd)
    {
        throw new NotImplementedException();
    }

    public ReadDirectoryResult ReadDirectory(Caller caller, FileDescriptor fd, Span<byte> buffer, long cookie, ref uint bufUsedOutput)
    {
        throw new NotImplementedException();
    }

    public WasiError PathCreateDirectory(Caller caller, FileDescriptor fd, ReadOnlySpan<byte> path)
    {
        throw new NotImplementedException();
    }

    public WasiError Write(Caller caller, FileDescriptor fd, ReadonlyBuffer<ReadonlyBuffer<byte>> iovs, ref uint nwrittenOutput)
    {
        throw new NotImplementedException();
    }

    public WasiError PWrite(Caller caller, FileDescriptor fd, Buffer<Buffer<byte>> iovs, long offset, ref uint nwrittenOutput)
    {
        throw new NotImplementedException();
    }

    public StatResult StatGet(Caller caller, FileDescriptor fd, ref FileStat result)
    {
        throw new NotImplementedException();
    }

    public WasiError StatSetSize(Caller caller, FileDescriptor fileDescriptor, long size)
    {
        throw new NotImplementedException();
    }

    public WasiError FdStatSetTimes(Caller caller, FileDescriptor fileDescriptor, long atime, long mtime, FstFlags fstFlags)
    {
        throw new NotImplementedException();
    }

    public WasiError PathFileStatSetTimes(Caller caller, FileDescriptor fileDescriptor, LookupFlags lookup, ReadOnlySpan<byte> path, long atime, long mtime, FstFlags fstFlags)
    {
        throw new NotImplementedException();
    }

    public StatResult PathStatGet(Caller caller, FileDescriptor fd, LookupFlags lookup, ReadOnlySpan<byte> path, ref FileStat result)
    {
        throw new NotImplementedException();
    }

    public ReadResult Read(Caller caller, FileDescriptor fd, Buffer<Buffer<byte>> iovs, ref uint nread)
    {
        throw new NotImplementedException();
    }

    public ReadResult PRead(Caller caller, FileDescriptor fd, Buffer<Buffer<byte>> iovs, long offset, ref uint nread)
    {
        throw new NotImplementedException();
    }

    public SeekResult Seek(Caller caller, FileDescriptor fd, long offset, Whence whence, ref ulong newOffset)
    {
        throw new NotImplementedException();
    }

    public SyncResult Sync(Caller caller, FileDescriptor fd)
    {
        throw new NotImplementedException();
    }

    public WasiError PathRemoveDirectory(Caller caller, FileDescriptor fd, ReadOnlySpan<byte> path)
    {
        throw new NotImplementedException();
    }

    public WasiError PathUnlinkFile(Caller caller, FileDescriptor fd, ReadOnlySpan<byte> path)
    {
        throw new NotImplementedException();
    }

    public WasiError PathRename(Caller caller, FileDescriptor fd, ReadOnlySpan<byte> oldPath, FileDescriptor newFd, ReadOnlySpan<byte> newPath)
    {
        throw new NotImplementedException();
    }

    public WasiError FdAllocate(Caller caller, FileDescriptor fd, long offset, long length)
    {
        throw new NotImplementedException();
    }

    public WasiError FdStatGet(Caller caller, FileDescriptor fd, ref FdStat pointer)
    {
        throw new NotImplementedException();
    }

    public WasiError FdStatSetFlags(Caller caller, FileDescriptor fd, FdFlags flags)
    {
        throw new NotImplementedException();
    }

    public WasiError ReadLinkAt(Caller caller, FileDescriptor fd, ReadOnlySpan<byte> path, Span<byte> result, ref int nwritten)
    {
        throw new NotImplementedException();
    }

    public WasiError PathLink(Caller caller, FileDescriptor sourceRootFd, ReadOnlySpan<byte> sourcePath, int lookupFlags, FileDescriptor destRootFd, ReadOnlySpan<byte> destPath)
    {
        throw new NotImplementedException();
    }

    public WasiError PathSymLink(Caller caller, ReadOnlySpan<byte> oldPath, FileDescriptor fileDescriptor, ReadOnlySpan<byte> newPath)
    {
        throw new NotImplementedException();
    }
}