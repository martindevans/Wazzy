using System.Runtime.InteropServices;
using Wazzy.WasiSnapshotPreview1.Clock;
using Wazzy.WasiSnapshotPreview1.FileSystem;

namespace Wazzy.WasiSnapshotPreview1.Poll;

[StructLayout(LayoutKind.Explicit, Size = 48)]
public struct WasiSubscription
{
    /// <summary>
    /// User-provided value that is attached to the subscription in the implementation and returned through event::userdata.
    /// </summary>
    [FieldOffset(0)]
    public ulong UserData;

    [FieldOffset(8)]
    public SubscriptionUnion Union;
}

[StructLayout(LayoutKind.Explicit, Size = 40)]
public readonly struct SubscriptionUnion
{
    [FieldOffset(0)]
    public readonly byte Tag;

    [FieldOffset(8)]
    private readonly SubscriptionClock _clock;

    [FieldOffset(8)]
    private readonly SubscriptionFdReadWrite _fdRead;

    [FieldOffset(8)]
    private readonly SubscriptionFdReadWrite _fdWrite;

    public SubscriptionClock GetClock()
    {
        if (Tag != 0)
            throw new InvalidOperationException($"Cannot get SubscriptionUnion with Tag '{Tag}' as SubscriptionClock");
        return _clock;
    }

    public SubscriptionFdReadWrite GetRead()
    {
        if (Tag != 1)
            throw new InvalidOperationException($"Cannot get SubscriptionUnion with Tag '{Tag}' as SubscriptionRead");
        return _fdRead;
    }

    public SubscriptionFdReadWrite GetWrite()
    {
        if (Tag != 2)
            throw new InvalidOperationException($"Cannot get SubscriptionUnion with Tag '{Tag}' as SubscriptionWrite");
        return _fdWrite;
    }
}

[StructLayout(LayoutKind.Explicit, Size = 32)]
public struct SubscriptionClock
{
    /// <summary>
    /// The clock against which to compare the timestamp.
    /// </summary>
    [FieldOffset(0)]
    public ClockId ID;

    /// <summary>
    /// The absolute or relative timestamp.
    /// </summary>
    [FieldOffset(8)]
    public ulong Timestamp;

    /// <summary>
    /// The amount of time that the implementation may wait additionally to coalesce with other events.
    /// </summary>
    [FieldOffset(16)]
    public ulong Precision;

    /// <summary>
    /// Flags specifying whether the timeout is absolute or relative
    /// </summary>
    [FieldOffset(24)]
    public SubClockFlags Flags;
}

[StructLayout(LayoutKind.Explicit, Size = 2)]
public struct SubClockFlags
{
    /// <summary>
    /// If set, treat the timestamp provided in subscription_clock::timeout as an absolute timestamp of clock subscription_clock::id.
    /// If clear, treat the timestamp provided in subscription_clock::timeout relative to the current time value of clock subscription_clock::id.
    /// </summary>
    [FieldOffset(0)]
    public ushort SubscriptionClockIsAbstime;
}

[StructLayout(LayoutKind.Explicit, Size = 4)]
public struct SubscriptionFdReadWrite
{
    [FieldOffset(0)]
    public FileDescriptor Fd;
}