using Wazzy.Interop;

namespace Wazzy.Tests;

[TestClass]
public sealed class InteropTests
    : IDisposable
{
    private readonly WasmTestHelper _helper = new("Scripts/Memory.wat");

    public void Dispose()
    {
        _helper.Dispose();
    }

    [TestMethod]
    public void ReadonlyBufferReadsData()
    {
        var instance = _helper.Instantiate();
        var memory = instance.GetMemory("memory")!;

        var buffer1 = new ReadonlyBuffer<byte>(0, 4);
        var span1 = buffer1.GetSpan(memory);
        CollectionAssert.AreEqual(
            new byte[] { 255, 255, 255, 255 },
            span1.ToArray()
        );

        var buffer2 = new ReadonlyBuffer<byte>(2, 4);
        var span2 = buffer2.GetSpan(memory);
        CollectionAssert.AreEqual(
            new byte[] { 255, 255, 0, 0 },
            span2.ToArray()
        );
    }

    [TestMethod]
    public void ReadonlyPointerReadsData()
    {
        var instance = _helper.Instantiate();
        var memory = instance.GetMemory("memory")!;

        var ptr1 = new ReadonlyPointer<int>(0);
        var value1 = ptr1.Deref(memory);
        Assert.AreEqual(-1, value1);
    }
}