using Wazzy.WasiSnapshotPreview1;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Builder;

namespace Wazzy.Tests.VFS;

[TestClass]
public class PathCreateDirectoryTests
{
    private const string ScriptPath = "VFS/Scripts/PathCreateDirectory.wat";

    [TestMethod]
    public void CreateDirectory_Success()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder().Build());

        var instance = helper.Instantiate();
        var result = instance.GetFunction<int>("create_directory_success")!();

        Assert.AreEqual((int)WasiError.SUCCESS, result);
    }

    [TestMethod]
    public void CreateDirectory_ReadonlyFilesystem_ReturnsEROFS()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .Readonly(true)
            .Build());

        var instance = helper.Instantiate();
        var result = instance.GetFunction<int>("create_directory_success")!();

        Assert.AreEqual((int)WasiError.EROFS, result);
    }

    [TestMethod]
    public void CreateDirectory_InvalidFd_ReturnsENOENT()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder().Build());

        var instance = helper.Instantiate();
        var result = instance.GetFunction<int>("create_directory_invalid_fd")!();

        Assert.AreEqual((int)WasiError.ENOENT, result);
    }

    [TestMethod]
    public void CreateDirectory_FdIsFile_ReturnsENOTDIR()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder().Build());

        var instance = helper.Instantiate();
        var result = instance.GetFunction<int>("create_directory_fd_is_file")!();

        Assert.AreEqual((int)WasiError.ENOTDIR, result);
    }

    [TestMethod]
    public void CreateDirectory_ParentPathNotFound_ReturnsENOENT()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder().Build());

        var instance = helper.Instantiate();
        var result = instance.GetFunction<int>("create_directory_parent_not_found")!();

        Assert.AreEqual((int)WasiError.ENOENT, result);
    }

    [TestMethod]
    public void CreateDirectory_AlreadyExists_ReturnsEEXIST()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root => root.CreateVirtualDirectory("already_exists"))
            .Build());

        var instance = helper.Instantiate();
        var result = instance.GetFunction<int>("create_directory_already_exists")!();

        Assert.AreEqual((int)WasiError.EEXIST, result);
    }
}
