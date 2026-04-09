using Wasmtime;
using Wazzy.WasiSnapshotPreview1;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Builder;

namespace Wazzy.Tests.VFS;

[TestClass]
public class FuzzReadWriteTests
{
    private const string ScriptPath = "VFS/Scripts/FuzzReadWrite.wat";

    // Offset in WAT memory where test data is staged for read/write operations.
    private const int DataBufBase = 256;

    // Maximum bytes transferred in a single read or write call.
    private const int MaxChunk = 128;

    // Upper bound on file size growth so the test stays fast.
    private const int MaxFileSize = 4096;

    // Number of random operations to execute.
    private const int Iterations = 2000;

    /// <summary>
    /// Fuzz the VFS by driving a sequence of random Write, PWrite, Read, PRead,
    /// Seek and Truncate operations.  A parallel oracle (byte[]) tracks the
    /// expected file contents and cursor position; every read is verified against it.
    /// </summary>
    [TestMethod]
    public void FuzzReadWrite()
    {
        const int seed = 42;

        using var helper = new WasmTestHelper(ScriptPath);
        helper.AddWasiFeature(new VirtualFileSystemBuilder()
            .WithVirtualRoot(root => root.CreateInMemoryFile("fuzz.txt"))
            .Build());
        var instance = helper.Instantiate();

        var memory       = instance.GetMemory("memory")!;
        var openFile     = instance.GetFunction<int>("open_file")!;
        var writeBuf     = instance.GetFunction<int, int, int, int>("write_buf")!;
        var pwriteBuf    = instance.GetFunction<int, int, int, long, int>("pwrite_buf")!;
        var readBuf      = instance.GetFunction<int, int, int, int>("read_buf")!;
        var preadBuf     = instance.GetFunction<int, int, int, long, int>("pread_buf")!;
        var getIoResult  = instance.GetFunction<int>("get_io_result")!;
        var seekFile     = instance.GetFunction<int, long, int, int>("seek_file")!;
        var getSeekResult = instance.GetFunction<long>("get_seek_result")!;
        var truncateFile = instance.GetFunction<int, long, int>("truncate_file")!;

        var fd = openFile();
        Assert.IsTrue(fd >= 0, "Failed to open fuzz.txt");

        var rng    = new Random(seed);
        var oracle = Array.Empty<byte>(); // mirrors the expected file contents
        long pos   = 0;                  // mirrors the expected file-position cursor

        for (var i = 0; i < Iterations; i++)
        {
            var opWeight = rng.Next(100);

            if (opWeight < 25)          // ~25 % – fd_write at current position
            {
                var len  = rng.Next(1, MaxChunk + 1);
                var data = RandomBytes(rng, len);

                WriteToMemory(memory, DataBufBase, data);
                var errno = writeBuf(fd, DataBufBase, len);

                Assert.AreEqual((int)WasiError.SUCCESS, errno, $"[{i}] write errno");
                Assert.AreEqual(len, getIoResult(), $"[{i}] write nwritten");

                oracle = OracleWrite(oracle, pos, data);
                pos += len;
            }
            else if (opWeight < 50)     // ~25 % – fd_pwrite at a random offset
            {
                var fileOffset = RandOffset(rng, oracle.Length);
                var len        = rng.Next(1, MaxChunk + 1);
                var data       = RandomBytes(rng, len);

                WriteToMemory(memory, DataBufBase, data);
                var errno = pwriteBuf(fd, DataBufBase, len, fileOffset);

                Assert.AreEqual((int)WasiError.SUCCESS, errno, $"[{i}] pwrite errno");
                Assert.AreEqual(len, getIoResult(), $"[{i}] pwrite nwritten");

                oracle = OracleWrite(oracle, fileOffset, data);
                // cursor position unchanged
            }
            else if (opWeight < 70)     // ~20 % – fd_read from current position
            {
                var len   = rng.Next(1, MaxChunk + 1);
                var errno = readBuf(fd, DataBufBase, len);

                Assert.AreEqual((int)WasiError.SUCCESS, errno, $"[{i}] read errno");

                var nread    = getIoResult();
                var expected = OracleRead(oracle, pos, nread);
                var actual   = ReadFromMemory(memory, DataBufBase, nread);

                CollectionAssert.AreEqual(expected, actual,
                    $"[{i}] read data mismatch at pos={pos}");

                pos += nread;
            }
            else if (opWeight < 90)     // ~20 % – fd_pread at a random offset
            {
                var fileOffset = RandOffset(rng, oracle.Length);
                var len        = rng.Next(1, MaxChunk + 1);
                var errno      = preadBuf(fd, DataBufBase, len, fileOffset);

                Assert.AreEqual((int)WasiError.SUCCESS, errno, $"[{i}] pread errno");

                var nread    = getIoResult();
                var expected = OracleRead(oracle, fileOffset, nread);
                var actual   = ReadFromMemory(memory, DataBufBase, nread);

                CollectionAssert.AreEqual(expected, actual,
                    $"[{i}] pread data mismatch at offset={fileOffset}");

                // cursor position unchanged
            }
            else if (opWeight < 95)     // ~5 % – fd_seek (SEEK_SET) to a random position
            {
                var newPos = RandOffset(rng, oracle.Length);
                var errno  = seekFile(fd, newPos, 0 /* SEEK_SET */);

                Assert.AreEqual((int)WasiError.SUCCESS, errno, $"[{i}] seek errno");
                Assert.AreEqual(newPos, getSeekResult(), $"[{i}] seek result");

                pos = newPos;
            }
            else                        // ~5 % – fd_filestat_set_size (truncate)
            {
                var newSize = (long)rng.Next(0, Math.Max(1, (int)Math.Min(oracle.Length + 1, MaxFileSize)));
                var errno   = truncateFile(fd, newSize);

                Assert.AreEqual((int)WasiError.SUCCESS, errno, $"[{i}] truncate errno");

                oracle = OracleTruncate(oracle, newSize);
            }
        }
    }

    // ── oracle helpers ────────────────────────────────────────────────────────

    /// <summary>Return a random offset in [0, max(1, fileLen + MaxChunk)] capped at MaxFileSize.</summary>
    private static long RandOffset(Random rng, int fileLen)
        => rng.Next(0, Math.Max(1, (int)Math.Min((long)fileLen + MaxChunk, MaxFileSize)));

    /// <summary>Apply a write to the oracle byte array, extending with zeros as needed.</summary>
    private static byte[] OracleWrite(byte[] oracle, long offset, byte[] data)
    {
        var requiredLen = offset + data.Length;
        if (requiredLen > oracle.Length)
        {
            var grown = new byte[requiredLen];
            oracle.CopyTo(grown, 0);
            oracle = grown;
        }
        data.AsSpan().CopyTo(oracle.AsSpan((int)offset));
        return oracle;
    }

    /// <summary>
    /// Return the bytes the oracle predicts will be returned by a read of
    /// exactly <paramref name="nread"/> bytes starting at <paramref name="offset"/>.
    /// </summary>
    private static byte[] OracleRead(byte[] oracle, long offset, int nread)
    {
        if (nread == 0 || offset >= oracle.Length)
            return Array.Empty<byte>();

        var available = (int)Math.Min(nread, oracle.Length - offset);
        return oracle.AsSpan((int)offset, available).ToArray();
    }

    /// <summary>Truncate or zero-extend the oracle to exactly <paramref name="newSize"/> bytes.</summary>
    private static byte[] OracleTruncate(byte[] oracle, long newSize)
    {
        var result  = new byte[newSize];
        var copyLen = (int)Math.Min(oracle.Length, newSize);
        oracle.AsSpan(0, copyLen).CopyTo(result.AsSpan());
        return result;
    }

    // ── memory helpers ────────────────────────────────────────────────────────

    private static byte[] RandomBytes(Random rng, int length)
    {
        var buf = new byte[length];
        rng.NextBytes(buf);
        return buf;
    }

    private static void WriteToMemory(Memory memory, int offset, byte[] data)
        => data.AsSpan().CopyTo(memory.GetSpan(offset, data.Length));

    private static byte[] ReadFromMemory(Memory memory, int offset, int length)
    {
        if (length == 0)
            return Array.Empty<byte>();
        return memory.GetSpan(offset, length).ToArray();
    }
}
