using Wasmtime;
using Wazzy.Interop;

namespace Wazzy.WasiSnapshotPreview1.Poll;

public abstract class BaseWasiEventPoll
    : IWasiFeature
{
    /// <summary>
    /// The module which the exports of this feature are defined in
    /// </summary>
    public static readonly string Module = "wasi_snapshot_preview1";

    protected abstract WasiError PollOneoff(Caller caller, ReadOnlySpan<WasiSubscription> @in, Span<WasiEvent> @out, out int neventsOut);

    public void DefineOn(Linker linker)
    {
        linker.DefineFunction(Module, "poll_oneoff",
            (Caller caller, int @in, int @out, int nsubscriptions, int nevents) => (int)PollOneoff(
                caller,
                new ReadonlyBuffer<WasiSubscription>(@in, unchecked((uint)nsubscriptions)).GetSpan(caller),
                new Buffer<WasiEvent>(@out, unchecked((uint)nsubscriptions)).GetSpan(caller),
                out new Pointer<int>(nevents).Deref(caller)
            )
        );
    }
}