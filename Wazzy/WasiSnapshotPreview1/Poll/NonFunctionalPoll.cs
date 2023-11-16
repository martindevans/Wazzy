using Wasmtime;

namespace Wazzy.WasiSnapshotPreview1.Poll;

public class NonFunctionalPoll
    : BaseWasiEventPoll
{
    protected override WasiError PollOneoff(Caller caller, ReadOnlySpan<WasiSubscription> @in, Span<WasiEvent> @out, out int neventsOut)
    {
        neventsOut = 0;
        return WasiError.ENOTCAPABLE;
    }
}