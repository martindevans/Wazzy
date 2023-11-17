using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Logging;
using Wasmtime;
using Wazzy.Interop;

namespace Wazzy.WasiSnapshotPreview1.FileSystem.Implementations;

public class PrintToLoggerFilesystem
    : BaseWasiFileSystem
{
    private readonly ILogger _logger;
    private readonly LogLevel? _stdout;
    private readonly LogLevel? _stderr;

    public PrintToLoggerFilesystem(ILogger logger, LogLevel? stdout = LogLevel.Information, LogLevel? stderr = LogLevel.Warning)
    {
        _logger = logger;
        _stdout = stdout;
        _stderr = stderr;
    }

    protected override WasiError Write(Caller caller, FileDescriptor fd, ReadonlyBuffer<ReadonlyBuffer<byte>> iovs, ref uint nwrittenOutput)
    {
        // stdout/stderr
        if (fd.Handle != 1 && fd.Handle != 2)
            return WasiError.EBADF;

        var builder = new StringBuilder();
        var iovecs = iovs.GetSpan(caller);
        var totalWritten = 0u;
        for (var i = 0; i < iovecs.Length; i++)
        {
            var span = iovecs[i].GetSpan(caller);

            builder.Append(Encoding.UTF8.GetString(span));
            totalWritten += (uint)span.Length;
        }

        var level = fd.Handle == 1 ? _stdout : _stderr;
        if (level.HasValue)
            _logger.Log(level.Value, builder.ToString());

        nwrittenOutput = totalWritten;
        return WasiError.SUCCESS;
    }

    protected override PrestatGetResult PrestatGet(Caller caller, FileDescriptor fd, ref Prestat result)
    {
        return PrestatGetResult.BadFileDescriptor;
    }

    protected override PrestatDirNameResult PrestatDirName(Caller caller, FileDescriptor fd, Span<byte> name)
    {
        return PrestatDirNameResult.BadFileDescriptor;
    }

    protected override PathOpenResult PathOpen(Caller caller, FileDescriptor fd, LookupFlags lookup, ReadOnlySpan<byte> path, OpenFlags openFlags, FileRights baseRights, FileRights inheritingRights, FdFlags fdFlags, ref FileDescriptor outputFd)
    {
        return PathOpenResult.BadFileDescriptor;
    }

    protected override CloseResult Close(Caller caller, FileDescriptor fd)
    {
        return CloseResult.BadFileDescriptor;
    }

    protected override ReadDirectoryResult ReadDirectory(Caller caller, FileDescriptor fd, Span<byte> buffer, long cookie, ref uint bufUsedOutput)
    {
        return ReadDirectoryResult.BadFileDescriptor;
    }

    protected override WasiError PathCreateDirectory(Caller caller, FileDescriptor fd, ReadOnlySpan<byte> path)
    {
        return WasiError.EBADF;
    }

    protected override WasiError PWrite(Caller caller, FileDescriptor fd, Buffer<Buffer<byte>> iovs, long offset, ref uint nread)
    {
        return WasiError.EBADF;
    }

    protected override StatResult StatGet(Caller caller, FileDescriptor fd, ref FileStat result)
    {
        return StatResult.BadFileDescriptor;
    }

    protected override WasiError StatSetSize(Caller caller, FileDescriptor fileDescriptor, long size)
    {
        return WasiError.EBADF;
    }

    protected override WasiError FdStatSetTimes(Caller caller, FileDescriptor fileDescriptor, long atime, long mtime, FstFlags fstFlags)
    {
        return WasiError.EBADF;
    }

    protected override WasiError PathFileStatSetTimes(Caller caller, FileDescriptor fileDescriptor, LookupFlags lookup, ReadOnlySpan<byte> path, long atime, long mtime, FstFlags fstFlags)
    {
        return WasiError.EBADF;
    }

    protected override StatResult PathStatGet(Caller caller, FileDescriptor fd, LookupFlags lookup, ReadOnlySpan<byte> path, ref FileStat result)
    {
        return StatResult.BadFileDescriptor;
    }

    protected override ReadResult Read(Caller caller, FileDescriptor fd, Buffer<Buffer<byte>> iovs, ref uint nread)
    {
        return ReadResult.BadFileDescriptor;
    }

    protected override ReadResult PRead(Caller caller, FileDescriptor fd, Buffer<Buffer<byte>> iovs, long offset, ref uint nread)
    {
        return ReadResult.BadFileDescriptor;
    }

    protected override SeekResult Seek(Caller caller, FileDescriptor fd, long offset, Whence whence, ref ulong newOffset)
    {
        return SeekResult.BadFileDescriptor;
    }

    protected override SyncResult Sync(Caller caller, FileDescriptor fd)
    {
        return SyncResult.BadFileDescriptor;
    }

    protected override WasiError PathRemoveDirectory(Caller caller, FileDescriptor fd, ReadOnlySpan<byte> path)
    {
        return WasiError.EBADF;
    }

    protected override WasiError PathUnlinkFile(Caller caller, FileDescriptor fd, ReadOnlySpan<byte> path)
    {
        return WasiError.EBADF;
    }

    protected override WasiError PathRename(Caller caller, FileDescriptor fd, ReadOnlySpan<byte> oldPath, FileDescriptor newFd, ReadOnlySpan<byte> newPath)
    {
        return WasiError.EBADF;
    }

    protected override WasiError FdAdvise(Caller caller, FileDescriptor fd, long offset, long filesize, Advice advice)
    {
        return WasiError.EBADF;
    }

    protected override WasiError FdAllocate(Caller caller, FileDescriptor fd, long offset, long length)
    {
        return WasiError.EBADF;
    }

    protected override WasiError FdStatGet(Caller caller, FileDescriptor fd, ref FdStat pointer)
    {
        return WasiError.EBADF;
    }

    protected override WasiError FdStatSetFlags(Caller caller, FileDescriptor fd, FdFlags flags)
    {
        return WasiError.EBADF;
    }

    protected override WasiError ReadLinkAt(Caller caller, FileDescriptor fd, ReadOnlySpan<byte> path, Span<byte> result, ref int nwritten)
    {
        return WasiError.EBADF;
    }

    protected override WasiError PathLink(Caller caller, FileDescriptor sourceRootFd, ReadOnlySpan<byte> sourcePath, int lookupFlags, FileDescriptor destRootFd, ReadOnlySpan<byte> destPath)
    {
        return WasiError.EBADF;
    }

    protected override WasiError PathSymLink(Caller caller, ReadOnlySpan<byte> oldPath, FileDescriptor fileDescriptor, ReadOnlySpan<byte> newPath)
    {
        return WasiError.EBADF;
    }
}