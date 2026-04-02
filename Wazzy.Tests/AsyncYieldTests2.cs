using Wasmtime;
using Wazzy.Async;
using Wazzy.Async.Extensions;
using Wazzy.Extensions;
using Wazzy.WasiSnapshotPreview1.Process;

namespace Wazzy.Tests;

[TestClass]
public sealed class AsyncYieldTests2
    : IDisposable
{
    private readonly WasmTestHelper _helper = new("Scripts/YieldAsync.wasm");

    [TestInitialize]
    public void Init()
    {
        _helper.Linker.DefineFeature(new AsyncifyYieldProcess());
    }

    public void Dispose()
    {
        _helper.Dispose();
    }

    [TestMethod]
    public void IsAsyncCapable()
    {
        var instance = _helper.Instantiate();

        Assert.IsTrue(instance.IsAsyncCapable());
    }

    [TestMethod]
    public void BasicState()
    {
        var instance = _helper.Instantiate();

        Assert.AreEqual(AsyncState.None, instance.GetAsyncState());
    }

    [TestMethod]
    public void SimpleAsyncCall()
    {
        var instance = _helper.Instantiate();

        var call = instance.GetFunction<int>("call_yield")!;
        var result = call();

        while (instance.GetAsyncState() == AsyncState.Suspending)
        {
            var stack = instance.StopUnwind();

            Assert.IsInstanceOfType<SchedYieldSuspend>(stack.SuspendReason);

            instance.StartRewind(stack);

            call();
        }

        Assert.AreEqual(AsyncState.None, instance.GetAsyncState());
    }
}