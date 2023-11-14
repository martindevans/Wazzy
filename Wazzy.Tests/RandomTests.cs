using Wasmtime;
using Wazzy.WasiSnapshotPreview1.Random;

namespace Wazzy.Tests;

[TestClass]
public class RandomTests
    : IDisposable
{
    private readonly WasmTestHelper _helper = new("Scripts/GetRandom.wat");

    public void Dispose()
    {
        _helper.Dispose();
    }

    private static (int, long) GetRandom(Instance instance)
    {
        var random = instance.GetFunction<(int, long)>("get_random_i64")!;
        return random();
    }

    [TestMethod]
    public void CryptoRandomSource()
    {
        _helper.AddWasiFeature(new CryptoRandomSource());
        var instance = _helper.Instantiate();

        var (erra, vala) = GetRandom(instance);
        Assert.AreEqual(0, erra);

        var (errb, valb) = GetRandom(instance);
        Assert.AreEqual(0, errb);

        Assert.AreNotEqual(vala, valb);
    }

    [TestMethod]
    public void FastRandomSource()
    {
        _helper.AddWasiFeature(new SeededRandomSource(7));
        var instance = _helper.Instantiate();

        var (erra, vala) = GetRandom(instance);
        Assert.AreEqual(0, erra);
        Assert.AreEqual(7557430045921534795, vala);

        var (errb, valb) = GetRandom(instance);
        Assert.AreEqual(0, errb);
        Assert.AreEqual(-5764130197907881473, valb);
    }
}