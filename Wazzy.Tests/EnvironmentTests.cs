using Wazzy.WasiSnapshotPreview1.Environment;

namespace Wazzy.Tests;

[TestClass]
public class EnvironmentTests
    : IDisposable
{
    private readonly WasmTestHelper _helper = new("Scripts/GetEnv.wat");

    public void Dispose()
    {
        _helper.Dispose();
    }

    [TestMethod]
    public void GetEnvSizes()
    {
        _helper.AddWasiFeature(new BasicEnvironment()
                              .SetEnvironmentVariable("FOO", "BAR")
                              .SetEnvironmentVariable("BASH", "the_old_value")
                              .SetEnvironmentVariable("BASH", "baz")
        );
        var instance = _helper.Instantiate();

        var (err, count, length) = instance.GetFunction<(int, int, int)>("test_environ_get_sizes")!();
            
        Assert.AreEqual(0, err);
        Assert.AreEqual(2, count);
        Assert.AreEqual(17, length);
    }

    [TestMethod]
    public void GetEnv()
    {
        _helper.AddWasiFeature(new BasicEnvironment()
                              .SetEnvironmentVariable("FOO", "bar")
                              .SetEnvironmentVariable("BASH", "BAZ")
        );
        var instance = _helper.Instantiate();
        var memory = instance.GetMemory("memory")!;

        var err = instance.GetFunction<int>("test_environ_get")!();

        Assert.AreEqual(0, err);

        // Check that the items are where they should be
        Assert.AreEqual(128, memory.ReadInt32(0));
        Assert.AreEqual(136, memory.ReadInt32(4));

        // Check that the data is what it should be
        var foo = memory.ReadNullTerminatedString(128);
        Assert.AreEqual(foo, "FOO=bar");

        var bash = memory.ReadNullTerminatedString(136);
        Assert.AreEqual(bash, "BASH=BAZ");
    }

    [TestMethod]
    public void GetArgSizes()
    {
        _helper.AddWasiFeature(new BasicEnvironment()
           .SetArgs("--foo", "--bar", "--bash=baz")
        );
        var instance = _helper.Instantiate();

        var (err, count, length) = instance.GetFunction<(int, int, int)>("test_args_get_sizes")!();

        Assert.AreEqual(0, err);
        Assert.AreEqual(3, count);
        Assert.AreEqual(23, length);
    }

    [TestMethod]
    public void GetArgs()
    {
        _helper.AddWasiFeature(new BasicEnvironment()
           .SetArgs("--foo", "--bar", "--bash=baz")
        );
        var instance = _helper.Instantiate();
        var memory = instance.GetMemory("memory")!;

        var err = instance.GetFunction<int>("test_args_get")!();

        Assert.AreEqual(0, err);

        // Check that the items are where they should be
        Assert.AreEqual(128, memory.ReadInt32(0));
        Assert.AreEqual(134, memory.ReadInt32(4));

        // Check that the data is what it should be
        var foo = memory.ReadNullTerminatedString(128);
        Assert.AreEqual(foo, "--foo");
        var bar = memory.ReadNullTerminatedString(134);
        Assert.AreEqual(bar, "--bar");
        var bash = memory.ReadNullTerminatedString(140);
        Assert.AreEqual(bash, "--bash=baz");
    }
}