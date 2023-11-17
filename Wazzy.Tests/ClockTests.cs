using Wasmtime;
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
    public void ManualClock()
    {
        var clock = new ManualClock(DateTime.UnixEpoch);
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
}