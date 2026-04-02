//using System.Text;
//using Wasmtime;
//using Wazzy.Extensions;
//using Wazzy.WasiSnapshotPreview1.Random;

//namespace Wazzy.Tests;

//[TestClass]
//public class FreezeThawTests
//    : IDisposable
//{
//    private readonly WasmTestHelper _helper = new("Scripts/GetRandom.wat");

//    public void Dispose()
//    {
//        _helper.Dispose();
//    }

//    [TestMethod]
//    public void FreezeWithoutCrashing()
//    {
//        _helper.AddWasiFeature(new CryptoRandomSource());
//        var instance = _helper.Instantiate();

//        var (erra, vala) = instance.GetFunction<(int, long)>("get_random_i64")!();
//        Assert.AreEqual(0, erra);

//        var output = new MemoryStream();
//        instance.Freeze(output);

//        Console.WriteLine(output.Position);

//        var store2 = new Store(_helper.Engine);
//        output.Seek(0, SeekOrigin.Begin);
//        var i2 = _helper.Module.Thaw(store2, output);
//    }


//    [TestMethod]
//    public void RoundTripMemory()
//    {
//        var m1 = new Memory(_helper.Store, 0, 123, false);
//        var output = new MemoryStream();
//        using (var writer = new BinaryWriter(output, Encoding.UTF8, true))
//            m1.SerializeMemory("name", writer);

//        output.Position = 0;
//        string name;
//        Memory m2;
//        using (var reader = new BinaryReader(output, Encoding.UTF8, true))
//            (name, m2) = InstanceExtensions.DeserializeMemory(_helper.Store, reader);

//        Assert.AreEqual("name", name);

//        var span1 = m1.GetSpan(0, checked((int)m1.GetLength()));
//        var span2 = m2.GetSpan(0, checked((int)m2.GetLength()));
//        Assert.IsTrue(span1.SequenceEqual(span2));
//    }
//}