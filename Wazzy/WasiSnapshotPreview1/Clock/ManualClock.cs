using Wasmtime;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem;

namespace Wazzy.WasiSnapshotPreview1.Clock;

/// <summary>
/// A clock for WASI that stays frozen in time unless explicity advanced in time
/// </summary>
public class ManualClock
    : IWasiClock, IVFSClock
{
    private readonly DateTime Epoch = DateTime.UnixEpoch;

    private readonly TimeSpan _resolution;

    public DateTime Now { get; private set; }

    public ulong NowNanos => FromRealTime(Now);

    /// <summary>
    /// Set the clock to start at a given time
    /// </summary>
    /// <param name="timeNow"></param>
    /// <param name="resolution">Clock resolution (in nanoseconds)</param>
    public ManualClock(DateTime? timeNow, TimeSpan resolution)
    {
        _resolution = resolution;
        Now = timeNow ?? DateTime.UtcNow;
    }

    public ManualClock()
        : this(DateTime.UtcNow, TimeSpan.FromMilliseconds(1))
    {
    }

    /// <summary>
    /// Advance time by a given amount
    /// </summary>
    /// <param name="elapsed">Amount of time to move forward</param>
    /// <exception cref="ArgumentException">Thrown if elapsed time is negative</exception>
    public void Tick(TimeSpan elapsed)
    {
        if (elapsed.Ticks < 0)
            throw new ArgumentException("Cannot go backwards in time", nameof(elapsed));

        Now += elapsed;
    }

    public ulong FromRealTime(DateTimeOffset time)
    {
        var now = time - Epoch;
        var nanos = (ulong)now.Ticks * 100;
        return nanos;
    }

    public WasiError TimeGet(Caller caller, ClockId id, ulong precision, out ulong retValue)
    {
        switch (id)
        {
            case ClockId.Monotonic:
            case ClockId.Realtime:
            case ClockId.ProcessCpuTime:
            case ClockId.ThreadCpuTime:
            {
                retValue = NowNanos;
                return WasiError.SUCCESS;
            }

            default:
                retValue = 0;
                return WasiError.EINVAL;
        }
    }

    public WasiError GetResolution(Caller caller, ClockId id, out ulong retValue)
    {
        switch (id)
        {
            case ClockId.Monotonic:
            case ClockId.Realtime:
            case ClockId.ProcessCpuTime:
            case ClockId.ThreadCpuTime:
                retValue = (ulong)_resolution.TotalNanoseconds;
                return WasiError.SUCCESS;

            default:
                retValue = 0;
                return WasiError.EINVAL;
        }
    }

    DateTimeOffset IVFSClock.ToRealTime(ulong time)
    {
        return Epoch + TimeSpan.FromTicks((long)time / 100);
    }

    ulong IVFSClock.GetTime()
    {
        var now = Now - Epoch;
        var nanos = (ulong)now.Ticks * 100;
        return nanos;
    }
}