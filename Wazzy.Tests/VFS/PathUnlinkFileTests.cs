using Wazzy.WasiSnapshotPreview1;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Builder;

namespace Wazzy.Tests.VFS;

[TestClass]
public class PathUnlinkFileTests
{
    private const string ScriptPath = "VFS/Scripts/PathUnlinkFile.wat";

    // Root preopened directory is always FD 3 in the VFS
    private const int RootFd = 3;

    // Memory offsets and lengths matching the data section in PathUnlinkFile.wat
    private const int TestFileOffset = 0;
    private const int TestFileLen = 8;       // "test.txt"

    private const int SubdirFileOffset = 16;
    private const int SubdirFileLen = 15;    // "subdir/test.txt"

    private const int NoParentFileOffset = 32;
    private const int NoParentFileLen = 18;  // "no_parent/test.txt"

    private const int DirNameOffset = 64;
    private const int DirNameLen = 6;        // "subdir"

    [TestMethod]
    public void UnlinkFile_Success()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root => root.CreateInMemoryFile("test.txt"))
            .Build());
        var instance = helper.Instantiate();

        var result = instance.GetFunction<int, int, int, int>("unlink_file")!(RootFd, TestFileOffset, TestFileLen);

        Assert.AreEqual((int)WasiError.SUCCESS, result);
    }

    [TestMethod]
    public void UnlinkFile_FileInSubdirectory_Success()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root =>
                root.CreateVirtualDirectory("subdir", sub =>
                    sub.CreateInMemoryFile("test.txt")))
            .Build());
        var instance = helper.Instantiate();

        var result = instance.GetFunction<int, int, int, int>("unlink_file")!(RootFd, SubdirFileOffset, SubdirFileLen);

        Assert.AreEqual((int)WasiError.SUCCESS, result);
    }

    [TestMethod]
    public void UnlinkFile_ReadonlyFilesystem_ReturnsEROFS()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .Readonly(true)
            .WithVirtualRoot(root => root.CreateInMemoryFile("test.txt"))
            .Build());
        var instance = helper.Instantiate();

        var result = instance.GetFunction<int, int, int, int>("unlink_file")!(RootFd, TestFileOffset, TestFileLen);

        Assert.AreEqual((int)WasiError.EROFS, result);
    }

    [TestMethod]
    public void UnlinkFile_InvalidFd_ReturnsENOENT()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root => root.CreateInMemoryFile("test.txt"))
            .Build());
        var instance = helper.Instantiate();

        // FD 99 is not a known handle in the VFS, so GetDirectory returns ENOENT
        var result = instance.GetFunction<int, int, int, int>("unlink_file")!(99, TestFileOffset, TestFileLen);

        Assert.AreEqual((int)WasiError.ENOENT, result);
    }

    [TestMethod]
    public void UnlinkFile_FdIsNotDirectory_ReturnsENOTDIR()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root => root.CreateInMemoryFile("test.txt"))
            .Build());
        var instance = helper.Instantiate();

        // FD 0 is stdin, which is a file handle rather than a directory handle
        var result = instance.GetFunction<int, int, int, int>("unlink_file")!(0, TestFileOffset, TestFileLen);

        Assert.AreEqual((int)WasiError.ENOTDIR, result);
    }

    [TestMethod]
    public void UnlinkFile_ParentDirectoryNotExists_ReturnsENOENT()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root => { })
            .Build());
        var instance = helper.Instantiate();

        // "no_parent/test.txt" — the "no_parent" directory does not exist
        var result = instance.GetFunction<int, int, int, int>("unlink_file")!(RootFd, NoParentFileOffset, NoParentFileLen);

        Assert.AreEqual((int)WasiError.ENOENT, result);
    }

    [TestMethod]
    public void UnlinkFile_FileNotExists_ReturnsENOENT()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root => { })
            .Build());
        var instance = helper.Instantiate();

        // Root directory exists but "test.txt" has not been created
        var result = instance.GetFunction<int, int, int, int>("unlink_file")!(RootFd, TestFileOffset, TestFileLen);

        Assert.AreEqual((int)WasiError.ENOENT, result);
    }

    [TestMethod]
    public void UnlinkFile_TargetIsDirectory_ReturnsEISDIR()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root => root.CreateVirtualDirectory("subdir"))
            .Build());
        var instance = helper.Instantiate();

        // "subdir" is a directory — path_unlink_file must not remove directories
        var result = instance.GetFunction<int, int, int, int>("unlink_file")!(RootFd, DirNameOffset, DirNameLen);

        Assert.AreEqual((int)WasiError.EISDIR, result);
    }
}
