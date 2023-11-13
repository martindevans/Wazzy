using System.Diagnostics;
using Wasmtime;

namespace Wazzy.WasiSnapshotPreview1.Clock;

/// <summary>
/// A clock for WASI that ticks automatically as real time passes
/// </summary>
public class RealtimeClock
    : BaseWasiClock
{
    private readonly Stopwatch _monotonic;

    // This value increments every time the Monotonic clock is read. This ensures that
    // there's at least 1 nanosecond of difference from one call to the next.
    private ulong _monotonicSkew;

    /// <summary>
    /// Get the current time in nanoseconds since the unix epoch
    /// </summary>
    public ulong NowNanos => unchecked((ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) * 1_000_000;

    public RealtimeClock()
    {
        _monotonic = new Stopwatch();
        _monotonic.Start();
    }

    public override WasiError TimeGet(Caller caller, ClockId id, ulong precision, out ulong retValue)
    {
        switch (id)
        {
            case ClockId.Realtime:
            {
                retValue = NowNanos;
                return WasiError.SUCCESS;
            }

            case ClockId.Monotonic:
            {
                var now = _monotonic.ElapsedMilliseconds;
                var nanos = (ulong)now * 1000000;
                retValue = nanos + unchecked(_monotonicSkew++);
                return WasiError.SUCCESS;
            }

            case ClockId.ProcessCpuTime:
            case ClockId.ThreadCpuTime:
            default:
                retValue = 0;
                return WasiError.EINVAL;
        }
    }

    public override WasiError GetResolution(Caller caller, ClockId id, out ulong retValue)
    {
        switch (id)
        {
            case ClockId.Realtime:
                // 55 milliseconds, expressed as nanos
                retValue = 55_000_000;
                return WasiError.SUCCESS;

            case ClockId.Monotonic:
                // 15 milliseconds, expressed as nanos
                retValue = 15_000_000;
                return WasiError.SUCCESS;

            case ClockId.ProcessCpuTime:
            case ClockId.ThreadCpuTime:
            default:
                retValue = 0;
                return WasiError.EINVAL;
        }
    }
}