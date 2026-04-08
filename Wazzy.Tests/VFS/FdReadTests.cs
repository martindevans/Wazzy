using System.Text;
using Wazzy.WasiSnapshotPreview1;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Builder;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Files;

namespace Wazzy.Tests.VFS;

[TestClass]
public class FdReadTests
{
    private const string ScriptPath = "VFS/Scripts/FdRead.wat";

    // Pre-opened file descriptors assigned by the VFS.
    private const int StdinFd = 0;
    private const int StdoutFd = 1;
    private const int RootDirFd = 3;
    private const int InvalidFd = 99;

    // Test file content used by open_and_read / open_and_pread tests.
    private static readonly byte[] FileContent = Encoding.UTF8.GetBytes("Hello");

    // ── fd_read ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Reading a regular, readable in-memory file returns SUCCESS and the
    /// expected number of bytes with the correct content in the data buffer.
    /// </summary>
    [TestMethod]
    public void Read_Success()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root => root.CreateInMemoryFile("test.txt", FileContent))
            .Build());
        var instance = helper.Instantiate();

        var errno = instance.GetFunction<int>("open_and_read")!();
        var nread = instance.GetFunction<int>("get_nread")!();

        Assert.AreEqual((int)WasiError.SUCCESS, errno);
        Assert.AreEqual(FileContent.Length, nread);

        // Verify that the data buffer holds the expected bytes.
        var getDataByte = instance.GetFunction<int, int>("get_data_byte")!;
        for (var i = 0; i < FileContent.Length; i++)
            Assert.AreEqual(FileContent[i], getDataByte(i), $"data[{i}] mismatch");
    }

    /// <summary>
    /// Attempting to read from an unknown file descriptor (fd=99) must return
    /// EBADF (ReadResult.BadFileDescriptor).
    /// </summary>
    [TestMethod]
    public void Read_BadFileDescriptor_ReturnsEBADF()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder().Build());
        var instance = helper.Instantiate();

        var errno = instance.GetFunction<int, int>("read_fd")!(InvalidFd);

        Assert.AreEqual((int)WasiError.EBADF, errno);
    }

    /// <summary>
    /// Attempting to read from a directory file descriptor (fd=3, the root
    /// pre-open) must return EISDIR (ReadResult.IsDirectory).
    /// </summary>
    [TestMethod]
    public void Read_Directory_ReturnsEISDIR()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder().Build());
        var instance = helper.Instantiate();

        var errno = instance.GetFunction<int, int>("read_fd")!(RootDirFd);

        Assert.AreEqual((int)WasiError.EISDIR, errno);
    }

    /// <summary>
    /// Reading from a file handle whose underlying IFile.IsReadable is false
    /// (e.g. a write-only pipe used as stdout) returns SUCCESS with 0 bytes
    /// read.  This exercises the early-return path in CoCreateReadTask.
    /// </summary>
    [TestMethod]
    public void Read_NonReadableFile_ReturnsSuccessWithZeroBytes()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        // Replace the default (readable) stdout with a write-only sink.
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithPipes(stdout: new StringBuilderLog(new System.Text.StringBuilder()))
            .Build());
        var instance = helper.Instantiate();

        var errno = instance.GetFunction<int, int>("read_fd")!(StdoutFd);
        var nread = instance.GetFunction<int>("get_nread")!();

        Assert.AreEqual((int)WasiError.SUCCESS, errno);
        Assert.AreEqual(0, nread);
    }

    // ── fd_pread ──────────────────────────────────────────────────────────────

    /// <summary>
    /// PRead from offset 0 behaves like a regular read and returns SUCCESS
    /// with the full file content.
    /// </summary>
    [TestMethod]
    public void PRead_Success()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root => root.CreateInMemoryFile("test.txt", FileContent))
            .Build());
        var instance = helper.Instantiate();

        var errno = instance.GetFunction<long, int>("open_and_pread")!(0L);
        var nread = instance.GetFunction<int>("get_nread")!();

        Assert.AreEqual((int)WasiError.SUCCESS, errno);
        Assert.AreEqual(FileContent.Length, nread);
    }

    /// <summary>
    /// PRead with a positive byte offset reads only the tail of the file
    /// starting from that offset and returns the correct byte values.
    /// </summary>
    [TestMethod]
    public void PRead_WithOffset_ReadsFromOffset()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root => root.CreateInMemoryFile("test.txt", FileContent))
            .Build());
        var instance = helper.Instantiate();

        const int offset = 2; // Skip "He", read "llo"
        var errno = instance.GetFunction<long, int>("open_and_pread")!((long)offset);
        var nread = instance.GetFunction<int>("get_nread")!();

        Assert.AreEqual((int)WasiError.SUCCESS, errno);

        var expectedBytes = FileContent.AsSpan(offset).ToArray();
        Assert.AreEqual(expectedBytes.Length, nread);

        var getDataByte = instance.GetFunction<int, int>("get_data_byte")!;
        for (var i = 0; i < expectedBytes.Length; i++)
            Assert.AreEqual(expectedBytes[i], getDataByte(i), $"data[{i}] mismatch");
    }

    /// <summary>
    /// After a fd_pread call the file position cursor must remain where it was
    /// before the call.  This is verified by:
    ///   1. Reading 2 bytes with fd_read (position advances to 2).
    ///   2. Calling fd_pread at offset 0 (reads from the beginning).
    ///   3. Calling fd_read again – it should resume from position 2, not 0.
    /// If fd_pread had advanced the position the final fd_read would return 0
    /// instead of (file_length - 2).
    /// </summary>
    [TestMethod]
    public void PRead_DoesNotAdvanceFilePosition()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root => root.CreateInMemoryFile("test.txt", FileContent))
            .Build());
        var instance = helper.Instantiate();

        // The WAT function internally does: small read (2 bytes) → pread at 0 → read.
        // It returns the errno of the final read; get_nread() gives its byte count.
        var errno = instance.GetFunction<int>("open_small_read_pread_read")!();
        var nread = instance.GetFunction<int>("get_nread")!();

        Assert.AreEqual((int)WasiError.SUCCESS, errno);
        // The small read consumed 2 bytes; the final read must see the remaining bytes.
        Assert.AreEqual(FileContent.Length - 2, nread);
    }

    /// <summary>
    /// Attempting to pread from an unknown file descriptor returns EBADF.
    /// </summary>
    [TestMethod]
    public void PRead_BadFileDescriptor_ReturnsEBADF()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder().Build());
        var instance = helper.Instantiate();

        var errno = instance.GetFunction<int, long, int>("pread_fd")!(InvalidFd, 0L);

        Assert.AreEqual((int)WasiError.EBADF, errno);
    }

    /// <summary>
    /// Attempting to pread from a directory file descriptor returns EISDIR.
    /// </summary>
    [TestMethod]
    public void PRead_Directory_ReturnsEISDIR()
    {
        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder().Build());
        var instance = helper.Instantiate();

        var errno = instance.GetFunction<int, long, int>("pread_fd")!(RootDirFd, 0L);

        Assert.AreEqual((int)WasiError.EISDIR, errno);
    }
}
