using Wazzy.WasiSnapshotPreview1.FileSystem;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Builder;

namespace Wazzy.Tests;

[TestClass]
public class VirtualFileSystemTests
{
    [TestMethod]
    public void ReadMappedZipEntry()
    {
        var vfs = (IWasiFileSystem)new VirtualFileSystemBuilder()
            .Readonly(true)
            .WithVirtualRoot(root =>
            {
                root.MapReadonlyZipArchiveDirectory("TestFolder", "TestFolder.zip");
            })
            .Build();
    }
}