using Wasmtime;
using Wazzy.Interop;

namespace Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Files;

/// <summary>
/// Base interface for a handle to an open file
/// </summary>
public interface IFileHandle
    : IFilesystemHandle
{
    /// <summary>
    /// The file this handle is for
    /// </summary>
    IFile File { get; }

    /// <summary>
    /// The position to read/write from in the file
    /// </summary>
    ulong Position { get; set; }

    /// <summary>
    /// Flags which this handle was opened with
    /// </summary>
    FdFlags Flags { get; }

    /// <summary>
    /// Size of the underlying file
    /// </summary>
    ulong Size { get; }

    /// <summary>
    /// Write the given bytes into the file at the `Position`
    /// </summary>
    /// <param name="bytes">Bytes to write</param>
    /// <param name="timestamp">Timestamp of "now", used to update file metadata</param>
    /// <returns>Count of written bytes</returns>
    uint Write(ReadOnlySpan<byte> bytes, ulong timestamp);

    /// <summary>
    /// Read bytes from the file into the buffer
    /// </summary>
    /// <param name="bytes">Byte span to read into</param>
    /// <param name="timestamp">Timestamp of "now", used to update file metadata</param>
    /// <returns>Count of read bytes</returns>
    uint Read(Span<byte> bytes, ulong timestamp);

    /// <summary>
    /// Change the "Position" property
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="whence"></param>
    /// <param name="finalOffset"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    SeekResult Seek(long offset, Whence whence, out ulong finalOffset)
    {
        switch (whence)
        {
            case Whence.Set when offset < 0:
                finalOffset = Position;
                return SeekResult.InvalidParameter;

            case Whence.Set:
                Position = (ulong)offset;
                finalOffset = Position;
                return SeekResult.Success;

            case Whence.Current:
                {
                    var dest = (long)Position + offset;
                    if (dest < 0)
                    {
                        finalOffset = Position;
                        return SeekResult.InvalidParameter;
                    }

                    Position = (ulong)dest;
                    finalOffset = Position;
                    return SeekResult.Success;
                }

            case Whence.End:
                {
                    var dest = (long)Size + offset;
                    if (dest < 0)
                    {
                        finalOffset = Position;
                        return SeekResult.InvalidParameter;
                    }

                    Position = (ulong)dest;
                    finalOffset = Position;
                    return SeekResult.Success;
                }

            default:
                throw new ArgumentOutOfRangeException(nameof(whence), whence, null);
        }
    }

    /// <summary>
    /// Set the size of this file, discarding any extra data that is no longer within the file.
    /// </summary>
    /// <param name="timestamp"></param>
    /// <param name="size"></param>
    void Truncate(ulong timestamp, long size = 0);

    /// <summary>
    /// Complete any pending modifications to this file, ensuring they are written to the underlying device
    /// </summary>
    void Sync();

    ulong PollReadableBytes();

    ulong PollWritableBytes();

    /// <summary>
    /// Write all of the iovs one by one
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="iovs"></param>
    /// <param name="timestamp"></param>
    /// <returns>bytes written</returns>
    uint Write(Caller caller, ReadonlyBuffer<ReadonlyBuffer<byte>> iovs, ulong timestamp)
    {
        var nwrittenOutput = 0u;

        var iovecs = iovs.GetSpan(caller);
        for (var i = 0; i < iovecs.Length; i++)
        {
            var span = iovecs[i].GetSpan(caller);
            var written = Write(span, timestamp);
            nwrittenOutput += written;

            if (written != span.Length)
                break;
        }

        return nwrittenOutput;
    }
}

internal abstract class BaseFileHandle<T>
    : IFileHandle
    where T : IFile
{
    public FdFlags Flags { get; private set; }

    IFilesystemEntry IFilesystemHandle.Content => File;

    IFile IFileHandle.File => File;

    public T File { get; }

    protected BaseFileHandle(T file, FdFlags flags)
    {
        File = file;
        Flags = flags;
    }

    public abstract ulong Position { get; set; }

    public abstract ulong Size { get; }

    public FileType FileType => File.FileType;

    public FileStat GetFileStat()
    {
        return new FileStat(0, 0,
            FileType,
            0,
            Size,
            File.AccessTime,
            File.ModificationTime,
            File.ChangeTime
        );
    }

    public FdStat GetStat()
    {
        return new FdStat(FileType, Flags);
    }

    public WasiError SetFlags(FdFlags flags)
    {
        Flags = flags;
        return WasiError.SUCCESS;
    }

    public abstract uint Read(Span<byte> bytes, ulong timestamp);

    public abstract void Truncate(ulong timestamp, long size);

    public abstract uint Write(ReadOnlySpan<byte> bytes, ulong timestamp);

    public abstract void Dispose();

    public virtual void Sync()
    {
    }

    public abstract ulong PollReadableBytes();

    public abstract ulong PollWritableBytes();
}