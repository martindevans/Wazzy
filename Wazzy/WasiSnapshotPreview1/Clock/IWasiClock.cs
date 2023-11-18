using Wasmtime;
using Wazzy.Interop;

namespace Wazzy.WasiSnapshotPreview1.Clock;

/// <summary>
/// Implements WASI clock functions
/// </summary>
public interface IWasiClock
    : IWasiFeature
{
    /// <summary>
    /// The module which the exports of this feature are defined in
    /// </summary>
    public static readonly string Module = "wasi_snapshot_preview1";

    /// <summary>
    /// Return the time value of a clock
    /// </summary>
    /// <param name="caller">Context for this call</param>
    /// <param name="id">ID of the clock to fetch</param>
    /// <param name="precision">The maximum lag (exclusive) that the returned time value may have, compared to its actual value</param>
    /// <param name="retValue"></param>
    /// <returns></returns>
    protected WasiError TimeGet(Caller caller, ClockId id, ulong precision, out ulong retValue);

    /// <summary>
    /// Return the resolution of a clock.
    /// Implementations are required to provide a non-zero value for supported clocks.
    /// For unsupported clocks, return `WasiError.EINVAL`
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="id"></param>
    /// <param name="retValue"></param>
    /// <returns></returns>
    protected WasiError GetResolution(Caller caller, ClockId id, out ulong retValue);

    /// <inheritdoc />
    void IWasiFeature.DefineOn(Linker linker)
    {
        linker.DefineFunction(Module, "clock_time_get",
            (Caller caller, int id, long precision, int resultAddr) => (int)TimeGet(
                caller,
                (ClockId)id,
                unchecked((ulong)precision),
                out new Pointer<ulong>(resultAddr).Deref(caller)
            )
        );

        linker.DefineFunction(Module, "clock_res_get",
            (Caller caller, int id, int resultAddr) => (int)GetResolution(
                caller,
                (ClockId)id,
                out new Pointer<ulong>(resultAddr).Deref(caller)
            )
        );
    }
}

public enum ClockId
    : uint
{
    /// <summary>The clock measuring real time. Time value zero corresponds with 1970-01-01T00:00:00Z</summary>
    Realtime = 0,

    /// <summary>
    /// The store-wide monotonic clock, which is defined as a clock measuring real time, whose value cannot be
    /// adjusted and which cannot have negative clock jumps. The epoch of this clock is undefined. The absolute time
    /// value of this clock therefore has no meaning
    /// </summary>
    Monotonic = 1,

    /// <summary>The CPU-time clock associated with the current process</summary>
    ProcessCpuTime = 2,

    /// <summary>The CPU-time clock associated with the current thread</summary>
    ThreadCpuTime = 3,
}