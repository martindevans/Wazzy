using Wasmtime;
using Wazzy.WasiSnapshotPreview1;
using Wazzy.WasiSnapshotPreview1.Process;
using Wazzy.WasiSnapshotPreview1.Random;

namespace Wazzy.Tests;

[TestClass]
public class ExitProcessTests
    : IDisposable
{
    private readonly WasmTestHelper _helper = new("Scripts/ExitProcess.wat");

    public void Dispose()
    {
        _helper.Dispose();
    }

    [TestMethod]
    public void ThrowExit()
    {
        _helper.AddWasiFeature(new ThrowExitProcess());
        var instance = _helper.Instantiate();

        var call = instance.GetAction<int>("call_exit")!;

        try
        {
            call(42);
        }
        catch (WasmtimeException ex)
        {
            Assert.IsInstanceOfType<ThrowExitProcessException>(ex.InnerException);
            Assert.AreEqual(42u, ((ThrowExitProcessException)ex.InnerException).ExitCode);
            return;
        }

        Assert.Fail("Didn't throw");
    }
}