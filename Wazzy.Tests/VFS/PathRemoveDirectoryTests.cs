using Wazzy.WasiSnapshotPreview1;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Builder;

namespace Wazzy.Tests.VFS;

[TestClass]
public class PathRemoveDirectoryTests
{
    private const string ScriptPath = "VFS/Scripts/PathRemoveDirectory.wat";

    // Root preopened directory is always FD 3 in the VFS
    private const int RootFd = 3;

    // Memory offsets and lengths matching the data section in PathRemoveDirectory.wat
    private const int TestDirOffset = 0;
    private const int TestDirLen = 7;        // "testdir"

    private const int NestedDirOffset = 16;
    private const int NestedDirLen = 12;     // "subdir/child"

    private const int NoParentOffset = 32;
    private const int NoParentLen = 17;      // "no_parent/testdir"

    private const int FileNameOffset = 64;
    private const int FileNameLen = 8;       // "test.txt"

    private const int NonemptyDirOffset = 80;
    private const int NonemptyDirLen = 8;    // "nonempty"

    [TestMethod]
    public void RemoveDirectory_Success()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root => root.CreateVirtualDirectory("testdir"))
            .Build());
        var instance = helper.Instantiate();

        var result = instance.GetFunction<int, int, int, int>("remove_directory")!(RootFd, TestDirOffset, TestDirLen);

        Assert.AreEqual((int)WasiError.SUCCESS, result);
    }

    [TestMethod]
    public void RemoveDirectory_DirectoryInSubdirectory_Success()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root =>
                root.CreateVirtualDirectory("subdir", sub =>
                    sub.CreateVirtualDirectory("child")))
            .Build());
        var instance = helper.Instantiate();

        var result = instance.GetFunction<int, int, int, int>("remove_directory")!(RootFd, NestedDirOffset, NestedDirLen);

        Assert.AreEqual((int)WasiError.SUCCESS, result);
    }

    [TestMethod]
    public void RemoveDirectory_ReadonlyFilesystem_ReturnsEROFS()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .Readonly(true)
            .WithVirtualRoot(root => root.CreateVirtualDirectory("testdir"))
            .Build());
        var instance = helper.Instantiate();

        var result = instance.GetFunction<int, int, int, int>("remove_directory")!(RootFd, TestDirOffset, TestDirLen);

        Assert.AreEqual((int)WasiError.EROFS, result);
    }

    [TestMethod]
    public void RemoveDirectory_InvalidFd_ReturnsEBADF()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root => root.CreateVirtualDirectory("testdir"))
            .Build());
        var instance = helper.Instantiate();

        // FD 99 is not known in the VFS, so GetHandle returns null → BadFileDescriptor
        var result = instance.GetFunction<int, int, int, int>("remove_directory")!(99, TestDirOffset, TestDirLen);

        Assert.AreEqual((int)WasiError.EBADF, result);
    }

    [TestMethod]
    public void RemoveDirectory_FdIsFileHandle_ReturnsEBADF()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root => root.CreateVirtualDirectory("testdir"))
            .Build());
        var instance = helper.Instantiate();

        // FD 0 is stdin, which is a file handle not a directory handle → BadFileDescriptor
        var result = instance.GetFunction<int, int, int, int>("remove_directory")!(0, TestDirOffset, TestDirLen);

        Assert.AreEqual((int)WasiError.EBADF, result);
    }

    [TestMethod]
    public void RemoveDirectory_ParentDirectoryNotExists_ReturnsENOENT()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root => { })
            .Build());
        var instance = helper.Instantiate();

        // "no_parent/testdir" — "no_parent" directory does not exist
        var result = instance.GetFunction<int, int, int, int>("remove_directory")!(RootFd, NoParentOffset, NoParentLen);

        Assert.AreEqual((int)WasiError.ENOENT, result);
    }

    [TestMethod]
    public void RemoveDirectory_DirectoryNotExists_ReturnsENOENT()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root => { })
            .Build());
        var instance = helper.Instantiate();

        // Root directory exists but "testdir" has not been created
        var result = instance.GetFunction<int, int, int, int>("remove_directory")!(RootFd, TestDirOffset, TestDirLen);

        Assert.AreEqual((int)WasiError.ENOENT, result);
    }

    [TestMethod]
    public void RemoveDirectory_TargetIsFile_ReturnsENOTDIR()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root => root.CreateInMemoryFile("test.txt"))
            .Build());
        var instance = helper.Instantiate();

        // "test.txt" is a file — path_remove_directory must not remove files
        var result = instance.GetFunction<int, int, int, int>("remove_directory")!(RootFd, FileNameOffset, FileNameLen);

        Assert.AreEqual((int)WasiError.ENOTDIR, result);
    }

    [TestMethod]
    public void RemoveDirectory_DirectoryNotEmpty_ReturnsENOTEMPTY()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root =>
                root.CreateVirtualDirectory("nonempty", sub =>
                    sub.CreateInMemoryFile("child.txt")))
            .Build());
        var instance = helper.Instantiate();

        // "nonempty" contains a file — path_remove_directory must not remove non-empty directories
        var result = instance.GetFunction<int, int, int, int>("remove_directory")!(RootFd, NonemptyDirOffset, NonemptyDirLen);

        Assert.AreEqual((int)WasiError.ENOTEMPTY, result);
    }
}
