using System.Diagnostics;
using Wasmtime;

namespace Wazzy.WasiSnapshotPreview1.Clock;

/// <summary>
/// A clock for WASI that ticks automatically as real time passes
/// </summary>
public class RealtimeClock
    : IWasiClock
{
    private readonly Stopwatch _monotonic;

    /// <summary>
    /// Get the current time in nanoseconds since the unix epoch
    /// </summary>
    public ulong NowNanos => unchecked((ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) * 1_000_000;

    public RealtimeClock()
    {
        _monotonic = new Stopwatch();
        _monotonic.Start();
    }

    public WasiError TimeGet(Caller caller, ClockId id, ulong precision, out ulong retValue)
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
                retValue = (ulong)_monotonic.Elapsed.TotalNanoseconds;
                return WasiError.SUCCESS;
            }

            case ClockId.ProcessCpuTime:
            case ClockId.ThreadCpuTime:
            default:
                retValue = 0;
                return WasiError.EINVAL;
        }
    }

    public WasiError GetResolution(Caller caller, ClockId id, out ulong retValue)
    {
        switch (id)
        {
            case ClockId.Realtime:
            case ClockId.Monotonic:
            case ClockId.ProcessCpuTime:
            case ClockId.ThreadCpuTime:
                // 10 milliseconds, expressed as nanos
                retValue = 10_000_000;
                return WasiError.SUCCESS;

            default:
                retValue = 0;
                return WasiError.EINVAL;
        }
    }
}