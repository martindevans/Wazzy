using Wasmtime;
using Wazzy.Extensions;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Builder;

namespace Wazzy.Tests;

[TestClass]
public class VfsTests
{
    [TestMethod]
    public void MapDirectory()
    {
        Assert.ThrowsException<ArgumentException>(() =>
        {
            new VirtualFileSystemBuilder()
                .WithVirtualRoot(builder => { builder.MapDirectory("", ".", true); })
                .Build();
        });
    }
}