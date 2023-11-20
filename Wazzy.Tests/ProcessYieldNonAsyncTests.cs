using Wasmtime;
using Wazzy.Async;
using Wazzy.Async.Extensions;
using Wazzy.Extensions;
using Wazzy.WasiSnapshotPreview1;
using Wazzy.WasiSnapshotPreview1.Process;

namespace Wazzy.Tests;

[TestClass]
public sealed class ProcessYieldNonAsyncTests
    : IDisposable
{
    private readonly WasmTestHelper _helper = new("Scripts/YieldNonAsync.wat");

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
    public void CallYieldNotCapable()
    {
        var instance = _helper.Instantiate();

        var call = instance.GetFunction<int>("call_yield")!;
        var result = call();

        Assert.AreEqual((int)WasiError.ENOTCAPABLE, result);
    }
}