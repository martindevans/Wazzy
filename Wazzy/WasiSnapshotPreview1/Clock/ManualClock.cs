using Wasmtime;

namespace Wazzy.WasiSnapshotPreview1.Clock;

/// <summary>
/// A clock for WASI that stays frozen in time unless explicity advanced in time
/// </summary>
public class ManualClock
    : BaseWasiClock
{
    private readonly DateTime Epoch = new(year: 1970, month: 1, day: 1);

    private DateTime _now;
    public DateTime Now
    {
        get => _now;
        set
        {
            if (value < _now)
                throw new InvalidOperationException("Cannot go backwards in time");
            _now = value;
        }
    }

    public ulong NowNanos => FromRealTime(Now);

    /// <summary>
    /// Set the clock to start at a given time
    /// </summary>
    /// <param name="timeNow"></param>
    public ManualClock(DateTime? timeNow = null)
    {
        _now = timeNow ?? DateTime.UtcNow;
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

    protected override WasiError TimeGet(Caller caller, ClockId id, ulong precision, out ulong retValue)
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

    protected override WasiError GetResolution(Caller caller, ClockId id, out ulong retValue)
    {
        switch (id)
        {
            case ClockId.Realtime:
                // 1 milliseconds, expressed as nanos
                retValue = 1_000_000;
                return WasiError.SUCCESS;

            case ClockId.Monotonic:
                // 1 milliseconds, expressed as nanos
                retValue = 1_000_000;
                return WasiError.SUCCESS;

            case ClockId.ProcessCpuTime:
            case ClockId.ThreadCpuTime:
            default:
                retValue = 0;
                return WasiError.EINVAL;
        }
    }
}