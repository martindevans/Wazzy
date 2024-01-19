using Wasmtime;
using Wazzy.Async.Extensions;
using Wazzy.Async;
using Wazzy.WasiSnapshotPreview1;
using Wazzy.WasiSnapshotPreview1.Clock;

namespace Wazzy.Tests;

[TestClass]
public sealed class ClockTests
    : IDisposable
{
    private readonly WasmTestHelper _helper = new("Scripts/GetTime.wat");

    public void Dispose()
    {
        _helper.Dispose();
    }

    private static (int, long) GetTime(Instance instance, int id)
    {
        return instance.GetFunction<int, (int, long)>("get_clock")!(id);
    }

    private static (int, long) GetResolution(Instance instance, int id)
    {
        return instance.GetFunction<int, (int, long)>("get_res")!(id);
    }

    [TestMethod]
    public void RealtimeClock()
    {
        _helper.AddWasiFeature(new RealtimeClock());
        var instance = _helper.Instantiate();

        // Get the time
        var (erra, firstTime) = GetTime(instance, 0);
        Assert.AreEqual(0, erra);

        // Let some real time pass
        Thread.Sleep(100);

        // Get the time again
        var (errb, secondTime) = GetTime(instance, 0);
        Assert.AreEqual(0, errb);

        // Check that some time passed
        Assert.AreNotEqual(firstTime, secondTime);

        // Check resolution
        var (reserr, res) = GetResolution(instance, 0);
        Assert.AreEqual(0, reserr);
        Assert.IsTrue(res > 0);
    }

    [TestMethod]
    public void RealtimeClockResolution()
    {
        _helper.AddWasiFeature(new RealtimeClock());
        var instance = _helper.Instantiate();

        // Check resolution is somewhere between 0 and 100ms
        var (reserr, res) = GetResolution(instance, 0);
        Assert.AreEqual(0, reserr);
        Assert.IsTrue(res is > 0 and < 100_000_000);
    }

    [TestMethod]
    public void RealtimeClockResolutionInvalidId()
    {
        _helper.AddWasiFeature(new RealtimeClock());
        var instance = _helper.Instantiate();

        var (reserr, _) = GetResolution(instance, 5);
        Assert.AreEqual((int)WasiError.EINVAL, reserr);
    }

    [TestMethod]
    public void MonotonicClock()
    {
        _helper.AddWasiFeature(new RealtimeClock());
        var instance = _helper.Instantiate();

        // Get the time
        var (erra, firstTime) = GetTime(instance, 1);
        Assert.AreEqual(0, erra);

        // Get the time again
        var (errb, secondTime) = GetTime(instance, 1);
        Assert.AreEqual(0, errb);

        // Check that not much time has passed between the two calls (1ms)
        Assert.IsTrue((firstTime - secondTime) < 1_000_000);
    }

    [TestMethod]
    public void RealtimeClockGetTimeInvalidId()
    {
        _helper.AddWasiFeature(new RealtimeClock());
        var instance = _helper.Instantiate();

        // Get the time
        var (erra, firstTime) = GetTime(instance, 5);
        Assert.AreEqual((int)WasiError.EINVAL, erra);
    }

    [TestMethod]
    public void ManualClock()
    {
        var clock = new ManualClock(DateTime.UnixEpoch, TimeSpan.FromMilliseconds(1));
        _helper.AddWasiFeature(clock);
        var instance = _helper.Instantiate();

        // Get the time
        var (erra, time1) = GetTime(instance, 0);
        Assert.AreEqual(0, erra);

        // Let some real time pass
        Thread.Sleep(100);

        // Get the time again
        var (errb, time2) = GetTime(instance, 0);
        Assert.AreEqual(0, errb);

        // Check that no time passed
        Assert.AreEqual(time1, time2);

        // Tick forward 14us
        clock.Tick(TimeSpan.FromMicroseconds(14));

        // Get the time yet again
        var (errc, time3) = GetTime(instance, 0);
        Assert.AreEqual(0, errc);

        // Check that exactly 14us passed
        Assert.AreEqual((long)TimeSpan.FromMicroseconds(14).TotalNanoseconds, time3 - time2);
    }

    [TestMethod]
    public void ManualTickBackwards()
    {
        var clock = new ManualClock(DateTime.UnixEpoch, TimeSpan.FromMilliseconds(1));
        _helper.AddWasiFeature(clock);

        Assert.ThrowsException<ArgumentException>(() =>
        {
            clock.Tick(TimeSpan.FromDays(-1));
        });
    }

    [TestMethod]
    public void ManualClockResolution()
    {
        _helper.AddWasiFeature(new ManualClock());
        var instance = _helper.Instantiate();

        // Check resolution is somewhere between 0 and 100ms
        var (reserr, res) = GetResolution(instance, 0);
        Assert.AreEqual(0, reserr);
        Assert.IsTrue(res is > 0 and < 100_000_000);
    }

    [TestMethod]
    public void ManualClockResolutionInvalidId()
    {
        _helper.AddWasiFeature(new ManualClock());
        var instance = _helper.Instantiate();

        var (reserr, _) = GetResolution(instance, 5);
        Assert.AreEqual((int)WasiError.EINVAL, reserr);
    }

    [TestMethod]
    public void ManualClockGetTimeInvalidId()
    {
        _helper.AddWasiFeature(new ManualClock());
        var instance = _helper.Instantiate();

        var (reserr, _) = GetTime(instance, 5);
        Assert.AreEqual((int)WasiError.EINVAL, reserr);
    }
}