using Wazzy.WasiSnapshotPreview1;
using Wazzy.WasiSnapshotPreview1.FileSystem;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Builder;

namespace Wazzy.Tests.VFS;

[TestClass]
public class PathRenameTests : IDisposable
{
    private readonly WasmTestHelper _helper = new("VFS/Scripts/PathRename.wat");

    public void Dispose()
    {
        _helper.Dispose();
    }

    private static IWasiFileSystem BuildVfs(
        Action<VirtualFileSystemBuilder>? configure = null,
        Action<DirectoryBuilder>? files = null)
    {
        var builder = new VirtualFileSystemBuilder();
        configure?.Invoke(builder);
        if (files != null)
            builder.WithVirtualRoot(files);
        return (IWasiFileSystem)builder.Build();
    }

    // ── Success cases ─────────────────────────────────────────────────────────

    [TestMethod]
    public void RenameFile_Success()
    {
        var vfs = BuildVfs(files: root =>
        {
            root.CreateInMemoryFile("source.txt");
        });

        _helper.AddWasiFeature(vfs);
        var instance = _helper.Instantiate();

        var result = instance.GetFunction<int>("rename_file")!();

        Assert.AreEqual((int)WasiError.SUCCESS, result);
    }

    [TestMethod]
    public void RenameDirectory_Success()
    {
        var vfs = BuildVfs(files: root =>
        {
            root.CreateVirtualDirectory("sourcedir");
        });

        _helper.AddWasiFeature(vfs);
        var instance = _helper.Instantiate();

        var result = instance.GetFunction<int>("rename_dir")!();

        Assert.AreEqual((int)WasiError.SUCCESS, result);
    }

    [TestMethod]
    public void MoveFile_ToSubdirectory_Success()
    {
        var vfs = BuildVfs(files: root =>
        {
            root.CreateInMemoryFile("source.txt");
            root.CreateVirtualDirectory("subdir");
        });

        _helper.AddWasiFeature(vfs);
        var instance = _helper.Instantiate();

        var result = instance.GetFunction<int>("move_to_subdir")!();

        Assert.AreEqual((int)WasiError.SUCCESS, result);
    }

    [TestMethod]
    public void MoveFile_FromSubdirectory_Success()
    {
        var vfs = BuildVfs(files: root =>
        {
            root.CreateVirtualDirectory("subdir", subdir =>
            {
                subdir.CreateInMemoryFile("source.txt");
            });
        });

        _helper.AddWasiFeature(vfs);
        var instance = _helper.Instantiate();

        var result = instance.GetFunction<int>("move_from_subdir")!();

        Assert.AreEqual((int)WasiError.SUCCESS, result);
    }

    // ── EROFS – read-only file system ─────────────────────────────────────────

    [TestMethod]
    public void RenameFile_ReadOnlyFilesystem_ReturnsEROFS()
    {
        var vfs = BuildVfs(
            configure: b => b.Readonly(true),
            files: root =>
            {
                root.CreateInMemoryFile("source.txt");
            });

        _helper.AddWasiFeature(vfs);
        var instance = _helper.Instantiate();

        var result = instance.GetFunction<int>("rename_file")!();

        Assert.AreEqual((int)WasiError.EROFS, result);
    }

    // ── ENOENT – bad file descriptor (handle not found) ───────────────────────

    [TestMethod]
    public void RenameFile_InvalidOldFd_ReturnsENOENT()
    {
        var vfs = BuildVfs(files: root =>
        {
            root.CreateInMemoryFile("source.txt");
        });

        _helper.AddWasiFeature(vfs);
        var instance = _helper.Instantiate();

        var result = instance.GetFunction<int>("rename_bad_old_fd")!();

        Assert.AreEqual((int)WasiError.ENOENT, result);
    }

    [TestMethod]
    public void RenameFile_InvalidNewFd_ReturnsENOENT()
    {
        var vfs = BuildVfs(files: root =>
        {
            root.CreateInMemoryFile("source.txt");
        });

        _helper.AddWasiFeature(vfs);
        var instance = _helper.Instantiate();

        var result = instance.GetFunction<int>("rename_bad_new_fd")!();

        Assert.AreEqual((int)WasiError.ENOENT, result);
    }

    // ── ENOTDIR – fd resolves to a file handle, not a directory ───────────────

    [TestMethod]
    public void RenameFile_OldFdIsFileHandle_ReturnsENOTDIR()
    {
        // fd=1 is stdout (a file handle). GetDirectory returns ENOTDIR when
        // the resolved handle is not an IDirectoryHandle.
        var vfs = BuildVfs(files: root =>
        {
            root.CreateInMemoryFile("source.txt");
        });

        _helper.AddWasiFeature(vfs);
        var instance = _helper.Instantiate();

        var result = instance.GetFunction<int>("rename_file_fd_as_old")!();

        Assert.AreEqual((int)WasiError.ENOTDIR, result);
    }

    // ── ENOENT – source or destination path cannot be resolved ────────────────

    [TestMethod]
    public void RenameFile_SourceFileNotFound_ReturnsENOENT()
    {
        // VFS is empty; "notfound.txt" does not exist.
        var vfs = BuildVfs();

        _helper.AddWasiFeature(vfs);
        var instance = _helper.Instantiate();

        var result = instance.GetFunction<int>("rename_nonexistent_source")!();

        Assert.AreEqual((int)WasiError.ENOENT, result);
    }

    [TestMethod]
    public void RenameFile_SourceParentDirectoryNotFound_ReturnsENOENT()
    {
        // "missing" directory does not exist so the parent cannot be resolved.
        var vfs = BuildVfs();

        _helper.AddWasiFeature(vfs);
        var instance = _helper.Instantiate();

        var result = instance.GetFunction<int>("rename_source_parent_missing")!();

        Assert.AreEqual((int)WasiError.ENOENT, result);
    }

    [TestMethod]
    public void RenameFile_DestinationParentDirectoryNotFound_ReturnsENOENT()
    {
        // Source exists but the destination parent ("missing") does not.
        var vfs = BuildVfs(files: root =>
        {
            root.CreateInMemoryFile("source.txt");
        });

        _helper.AddWasiFeature(vfs);
        var instance = _helper.Instantiate();

        var result = instance.GetFunction<int>("rename_dest_parent_missing")!();

        Assert.AreEqual((int)WasiError.ENOENT, result);
    }

    // ── EEXIST – destination name already occupied ────────────────────────────

    [TestMethod]
    public void RenameFile_DestinationAlreadyExists_ReturnsEEXIST()
    {
        // Both "source.txt" and "dest.txt" exist; the VFS does not overwrite.
        var vfs = BuildVfs(files: root =>
        {
            root.CreateInMemoryFile("source.txt");
            root.CreateInMemoryFile("dest.txt");
        });

        _helper.AddWasiFeature(vfs);
        var instance = _helper.Instantiate();

        var result = instance.GetFunction<int>("rename_dest_exists")!();

        Assert.AreEqual((int)WasiError.EEXIST, result);
    }
}
