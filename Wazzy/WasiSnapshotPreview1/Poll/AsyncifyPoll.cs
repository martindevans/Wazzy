using System.Buffers;
using Wasmtime;
using Wazzy.Async.Extensions;
using Wazzy.Async;
using Wazzy.WasiSnapshotPreview1.Clock;

namespace Wazzy.WasiSnapshotPreview1.Poll;

public class AsyncifyPoll
    : IWasiEventPoll
{
    private readonly IWasiClock _clock;

    public AsyncifyPoll(IWasiClock clock)
    {
        _clock = clock;
    }

    public WasiError PollOneoff(Caller caller, ReadOnlySpan<WasiSubscription> @in, Span<WasiEvent> @out, out int neventsOut)
    {
        // It's an error to poll zero things
        if (@in.Length == 0)
        {
            neventsOut = 0;
            return WasiError.EINVAL;
        }

        // Immediately exit if this caller can't async suspend
        if (!caller.IsAsyncCapable())
        {
            neventsOut = 0;
            return WasiError.ENOTCAPABLE;
        }

        // Get or restore locals. First time through this will save the current value of the clock.
        // Later calls can refer back to that and decide if it's timed out yet.
        var locals = caller.GetSuspendedLocals<PollOneoffLocals>()
                  ?? new PollOneoffLocals(caller, _clock);

        // Resume execution, if it was previously suspended
        caller.Resume(out var eState);

        // Check if any events happened, if they did return immediately
        if (CheckEvents(caller, locals, @in, @out, out neventsOut, out var task))
            return WasiError.SUCCESS;

        // Nothing happened! suspend execution
        caller.Suspend(locals, eState, reason: TaskSuspend.Create(task));
        return WasiError.SUCCESS;
    }

    private bool CheckEvents(Caller caller, PollOneoffLocals locals, ReadOnlySpan<WasiSubscription> @in, Span<WasiEvent> @out, out int eventCountOut, out Task? task)
    {
        eventCountOut = 0;

        var index = 0;
        var tasks = ArrayPool<Task>.Shared.Rent(@in.Length);
        var anyEvent = false;
        foreach (var item in @in)
        {
            // Try to get an event for this subscription
            Task? eventTask = null;
            var @event = item.Union.Tag switch
            {
                0 => CheckClock(caller, item.Union.GetClock(), out eventTask),
                1 => CheckRead(item.Union.GetRead(), out eventTask),
                2 => CheckWrite(item.Union.GetWrite(), out eventTask),
                _ => CheckUnknown(item.Union.Tag)
            };

            if (eventTask != null)
                tasks[index++] = eventTask;

            // Write out the event (if there is one) to the output buffer
            if (@event.HasValue)
            {
                @out[eventCountOut] = @event.Value;
                @out[eventCountOut].UserData = item.UserData;
                eventCountOut++;
                anyEvent = true;
            }
        }

        if (index > 0)
            task = Task.WhenAny(tasks.AsMemory(0, index).ToArray());
        else
            task = null;
        ArrayPool<Task>.Shared.Return(tasks, true);

        return anyEvent;

        WasiEvent? CheckClock(Caller caller, SubscriptionClock subscription, out Task? task)
        {
            // Try to get the current time from the clock
            var err = _clock.TimeGet(caller, subscription.ID, subscription.Precision, out var time);
            if (err != WasiError.SUCCESS)
            {
                task = null;
                return new WasiEvent
                {
                    Error = err,
                    Type = WasiEventType.Clock,
                };
            }

            // Check if it's an absolute timestamp
            if (subscription.Flags.SubscriptionClockIsAbstime != 0)
            {
                if (time < subscription.Timestamp)
                {
                    task = Task.Delay(TimeSpan.FromMilliseconds((subscription.Timestamp - time) / 1_000_000f));
                    return null;
                }

                task = null;
                return new WasiEvent
                {
                    Error = 0,
                    Type = WasiEventType.Clock,
                };
            }

            // Check timestamp (relative)
            var end = locals.Get(subscription.ID) + subscription.Timestamp;
            if (time < end)
            {
                task = Task.Delay(TimeSpan.FromMilliseconds((end - time) / 1_000_000f));
                return null;
            }

            task = null;
            return new WasiEvent
            {
                Error = 0,
                Type = WasiEventType.Clock,
            };
        }

        WasiEvent? CheckRead(SubscriptionFdReadWrite read, out Task? task)
        {
            task = null;
            return null;

            //todo: poll CheckRead

            //if (_vfs == null)
            //{
            //    return new WasiEvent
            //    {
            //        Error = WasiError.ENOSYS,
            //        Type = WasiEventType.FdRead,
            //    };
            //}

            //// Ask VFS
            //var result = _vfs.PollReadableBytes(read.Fd, out var data);

            //// Early exit if it failed
            //if (result != WasiError.SUCCESS)
            //{
            //    return new WasiEvent
            //    {
            //        Error = result,
            //        Type = WasiEventType.FdRead,
            //    };
            //}

            //// Don't return an event indicating zero, we're waiting until it's non-zero
            //if (data == 0)
            //    return null;

            //// Success! return event
            //return new WasiEvent
            //{
            //    Error = result,
            //    Type = WasiEventType.FdRead,
            //    fd_readwrite = new EventFdReadWrite
            //    {
            //        FileSize = data
            //    }
            //};
        }

        WasiEvent? CheckWrite(SubscriptionFdReadWrite write, out Task? task)
        {
            task = null;
            return null;

            //todo: poll CheckWrite

            //if (_vfs == null)
            //{
            //    return new WasiEvent
            //    {
            //        Error = WasiError.ENOSYS,
            //        Type = WasiEventType.Clock,
            //    };
            //}

            //// Ask VFS
            //var result = _vfs.PollWritableBytes(write.Fd, out var data);

            //// Early exit if it failed
            //if (result != WasiError.SUCCESS)
            //{
            //    return new WasiEvent
            //    {
            //        Error = result,
            //        Type = WasiEventType.Clock,
            //    };
            //}

            //// Don't return an event indicating zero, we're waiting until it's non-zero
            //if (data == 0)
            //    return null;

            //// Success! return event
            //return new WasiEvent
            //{
            //    Error = result,
            //    Type = WasiEventType.FdRead,
            //    fd_readwrite = new EventFdReadWrite
            //    {
            //        FileSize = data
            //    }
            //};
        }

        WasiEvent? CheckUnknown(int tag)
        {
            // Unknown poll_oneoff subscription type. Tag value: {tag}
            return null;
        }
    }

    private readonly struct PollOneoffLocals
    {
        private readonly ulong Realtime;
        private readonly ulong Monotonic;
        private readonly ulong ProcessCpuTime;
        private readonly ulong ThreadCpuTime;

        public PollOneoffLocals(Caller caller, IWasiClock clock)
        {
            clock.TimeGet(caller, ClockId.Realtime, 0, out Realtime);
            clock.TimeGet(caller, ClockId.Monotonic, 0, out Monotonic);
            clock.TimeGet(caller, ClockId.ProcessCpuTime, 0, out ProcessCpuTime);
            clock.TimeGet(caller, ClockId.ThreadCpuTime, 0, out ThreadCpuTime);
        }

        public ulong Get(ClockId id)
        {
            return id switch
            {
                ClockId.Realtime => Realtime,
                ClockId.Monotonic => Monotonic,
                ClockId.ProcessCpuTime => ProcessCpuTime,
                ClockId.ThreadCpuTime => ThreadCpuTime,
                _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
            };
        }
    }
}