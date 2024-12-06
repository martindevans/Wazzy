using System.Text;

namespace Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Files;

/// <summary>
/// File which writes everything written to it into a stringbuilder
/// </summary>
public class StringBuilderLog
    : IFile
{
    private class Handle
        : BaseFileHandle<StringBuilderLog>
    {
        public override ulong Position
        {
            get => 0;
            set { }
        }

        public override ulong Size => 0;

        public Handle(StringBuilderLog log, FdFlags flags)
            : base(log, flags)
        {
        }

        public override void Dispose()
        {
            Sync();
        }

        public override Task<uint> Read(Memory<byte> bytes, ulong timestamp)
        {
            throw new NotSupportedException("Cannot read StringBuilderLog");
        }

        public override void Truncate(ulong timestamp, long size)
        {
            throw new NotSupportedException("Cannot truncate StringBuilderLog");
        }

        public override uint Write(ReadOnlySpan<byte> bytes, ulong timestamp)
        {
            File.AccessTime = timestamp;
            File.ModificationTime = timestamp;
            File.ChangeTime = timestamp;

            // Process data in small chunks
            var bytesWritten = 0u;
            while (bytes.Length > 0)
            {
                const int CHUNK_SIZE = 128;
                if (bytes.Length < CHUNK_SIZE)
                {
                    bytesWritten += WriteBytes(bytes);
                    break;
                }

                var chunk = bytes[..CHUNK_SIZE];
                bytesWritten += WriteBytes(chunk);

                bytes = bytes[CHUNK_SIZE..];
            }

            return bytesWritten;
        }

        private uint WriteBytes(ReadOnlySpan<byte> bytes)
        {
            var charCount = Encoding.UTF8.GetCharCount(bytes);
            Span<char> chars = stackalloc char[charCount];
            charCount = Encoding.UTF8.GetChars(bytes, chars);

            File.Builder.Append(chars[..charCount]);

            return (uint)bytes.Length;
        }

        public override ulong PollReadableBytes()
        {
            return 0;
        }

        public override ulong PollWritableBytes()
        {
            return ushort.MaxValue;
        }
    }

    public bool CanMove => true;

    public FileType FileType => FileType.CharacterDevice;

    private StringBuilder Builder { get; }

    public StringBuilderLog(StringBuilder builder)
    {
        Builder = builder;
    }

    public ulong AccessTime { get; set; }
    public ulong ModificationTime { get; set; }
    public ulong ChangeTime { get; set; }

    public bool IsReadable => false;
    public bool IsWritable => true;

    IFileHandle IFile.Open(FdFlags flags)
    {
        return new Handle(this, flags);
    }

    public void Delete()
    {
    }

    public void MoveTo(Stream stream, ulong timestamp)
    {
        AccessTime = timestamp;
    }

    public IFilesystemEntry ToInMemory()
    {
        throw new InvalidOperationException();
    }
}