using Wazzy.WasiSnapshotPreview1;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Builder;

namespace Wazzy.Tests.VFS;

[TestClass]
public class FdWriteTests
{
    private const string ScriptPath = "VFS/Scripts/FdWrite.wat";

    // ── fd_write ──────────────────────────────────────────────────────────────

    /// <summary>
    /// fd_write to a writable in-memory file returns SUCCESS and reports the
    /// correct number of bytes written.
    /// </summary>
    [TestMethod]
    public void Write_Success_ReturnsSuccessAndNWritten()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root => root.CreateInMemoryFile("test.txt"))
            .Build());
        var instance = helper.Instantiate();

        var result  = instance.GetFunction<int>("write_success")!();
        var nwritten = instance.GetFunction<int>("get_nwritten")!();

        Assert.AreEqual((int)WasiError.SUCCESS, result);
        Assert.AreEqual(5, nwritten); // "hello" = 5 bytes
    }

    /// <summary>
    /// fd_write with an unregistered file descriptor (99) returns EBADF.
    /// </summary>
    [TestMethod]
    public void Write_BadFd_ReturnsEBADF()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root => { })
            .Build());
        var instance = helper.Instantiate();

        var result = instance.GetFunction<int>("write_bad_fd")!();

        Assert.AreEqual((int)WasiError.EBADF, result);
    }

    /// <summary>
    /// fd_write where the fd refers to a directory (fd=3, root pre-open)
    /// returns EISDIR.
    /// </summary>
    [TestMethod]
    public void Write_DirectoryFd_ReturnsEISDIR()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root => { })
            .Build());
        var instance = helper.Instantiate();

        var result = instance.GetFunction<int>("write_directory_fd")!();

        Assert.AreEqual((int)WasiError.EISDIR, result);
    }

    /// <summary>
    /// fd_write to a file created with isReadOnly=true returns EPERM.
    /// </summary>
    [TestMethod]
    public void Write_ReadOnlyFile_ReturnsEPERM()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root => root.CreateInMemoryFile("readonly.txt", isReadOnly: true))
            .Build());
        var instance = helper.Instantiate();

        var result = instance.GetFunction<int>("write_readonly")!();

        Assert.AreEqual((int)WasiError.EPERM, result);
    }

    // ── fd_pwrite ─────────────────────────────────────────────────────────────

    /// <summary>
    /// fd_pwrite to a writable in-memory file at offset 0 returns SUCCESS and
    /// reports the correct number of bytes written.
    /// </summary>
    [TestMethod]
    public void PWrite_Success_ReturnsSuccessAndNWritten()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root => root.CreateInMemoryFile("test.txt"))
            .Build());
        var instance = helper.Instantiate();

        var result   = instance.GetFunction<int>("pwrite_success")!();
        var nwritten = instance.GetFunction<int>("get_nwritten")!();

        Assert.AreEqual((int)WasiError.SUCCESS, result);
        Assert.AreEqual(5, nwritten); // "hello" = 5 bytes
    }

    /// <summary>
    /// fd_pwrite with an unregistered file descriptor (99) returns EBADF.
    /// </summary>
    [TestMethod]
    public void PWrite_BadFd_ReturnsEBADF()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root => { })
            .Build());
        var instance = helper.Instantiate();

        var result = instance.GetFunction<int>("pwrite_bad_fd")!();

        Assert.AreEqual((int)WasiError.EBADF, result);
    }

    /// <summary>
    /// fd_pwrite where the fd refers to a directory (fd=3, root pre-open)
    /// returns EISDIR.
    /// </summary>
    [TestMethod]
    public void PWrite_DirectoryFd_ReturnsEISDIR()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root => { })
            .Build());
        var instance = helper.Instantiate();

        var result = instance.GetFunction<int>("pwrite_directory_fd")!();

        Assert.AreEqual((int)WasiError.EISDIR, result);
    }

    /// <summary>
    /// fd_pwrite to a file created with isReadOnly=true returns EPERM.
    /// </summary>
    [TestMethod]
    public void PWrite_ReadOnlyFile_ReturnsEPERM()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root => root.CreateInMemoryFile("readonly.txt", isReadOnly: true))
            .Build());
        var instance = helper.Instantiate();

        var result = instance.GetFunction<int>("pwrite_readonly")!();

        Assert.AreEqual((int)WasiError.EPERM, result);
    }

    /// <summary>
    /// fd_pwrite with a negative offset returns EINVAL because seeking to a
    /// negative absolute position is invalid.
    /// </summary>
    [TestMethod]
    public void PWrite_NegativeOffset_ReturnsEINVAL()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root => root.CreateInMemoryFile("test.txt"))
            .Build());
        var instance = helper.Instantiate();

        var result = instance.GetFunction<int>("pwrite_negative_offset")!();

        Assert.AreEqual((int)WasiError.EINVAL, result);
    }
}
