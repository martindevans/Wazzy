using System.Runtime.InteropServices;

namespace Wazzy.WasiSnapshotPreview1.Poll;

[StructLayout(LayoutKind.Explicit, Size = 32)]
public struct WasiEvent
{
    /// <summary>
    /// User-provided value that got attached to subscription::userdata.
    /// </summary>
    [FieldOffset(0)]
    public ulong UserData;

    /// <summary>
    /// If non-zero, an error that occurred while processing the subscription request.
    /// </summary>
    [FieldOffset(8)]
    public WasiError Error;

    /// <summary>
    /// The type of event that occured
    /// </summary>
    [FieldOffset(10)]
    public WasiEventType Type;

    /// <summary>
    /// The contents of the event, if it is an eventtype::fd_read or eventtype::fd_write. eventtype::clock events ignore this field.
    /// </summary>
    [FieldOffset(16)]
    public EventFdReadWrite fd_readwrite;
}

public enum WasiEventType
    : byte
{
    /// <summary>
    /// The time value of clock subscription_clock::id has reached timestamp subscription_clock::timeout.
    /// </summary>
    Clock = 0,

    /// <summary>
    /// File descriptor subscription_fd_readwrite::file_descriptor has data available for reading. This event always triggers for regular files.
    /// </summary>
    FdRead = 1,

    /// <summary>
    ///  File descriptor subscription_fd_readwrite::file_descriptor has capacity available for writing. This event always triggers for regular files.
    /// </summary>
    FdWrite = 2,
}

[StructLayout(LayoutKind.Explicit, Size = 16)]
public struct EventFdReadWrite
{
    /// <summary>
    /// The number of bytes available for reading or writing.
    /// </summary>
    [FieldOffset(0)]
    public ulong FileSize;

    /// <summary>
    /// The state of the file descriptor. Note: currently unused.
    /// </summary>
    [FieldOffset(8)]
    private readonly ushort Flags;
}