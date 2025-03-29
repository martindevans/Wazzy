namespace Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Files;

public class InMemoryFile
        : IFile
{
    private class Handle
        : BaseFileHandle<InMemoryFile>
    {
        private long _position;

        public override ulong Size => (ulong)File._memory.Length;

        public override ulong Position
        {
            get => (ulong)_position;
            set
            {
                // Grow to ensure we can seek to this position
                if (value > Size)
                    File._memory.SetLength((long)value);
                _position = (long)value;
            }
        }

        public Handle(InMemoryFile file, FdFlags flags)
            : base(file, flags)
        {
            _position = 0;
        }

        public override void Dispose()
        {
        }

        public override void Truncate(ulong timestamp, long size)
        {
            TryWrite(timestamp);
            File._memory.SetLength(size);
        }

        public override Task<uint> Read(Memory<byte> memory, ulong timestamp)
        {
            TryRead(timestamp);

            File._memory.Seek(_position, SeekOrigin.Begin);
            var read = File._memory.Read(memory.Span);
            _position += read;

            return Task.FromResult((uint)read);
        }

        public override Task<uint> Write(ReadOnlyMemory<byte> bytes, ulong timestamp)
        {
            TryWrite(timestamp);

            if ((Flags & FdFlags.Append) != 0)
                _position = File._memory.Length;

            File._memory.Seek(_position, SeekOrigin.Begin);
            File._memory.Write(bytes.Span);

            var written = bytes.Length;
            _position += written;

            return Task.FromResult((uint)written);
        }

        private void TryRead(ulong timestamp)
        {
            if (!File.IsReadable)
                throw new InvalidOperationException("Cannot read from this file descriptor");

            File.AccessTime = timestamp;
        }

        private void TryWrite(ulong timestamp)
        {
            if (!File.IsWritable)
                throw new InvalidOperationException("Cannot write to this file descriptor");

            File.AccessTime = timestamp;
            File.ModificationTime = timestamp;
            File.ChangeTime = timestamp;
        }

        public override ulong PollReadableBytes()
        {
            return Size - Position;
        }

        public override ulong PollWritableBytes()
        {
            return ushort.MaxValue;
        }
    }

    private readonly MemoryStream _memory;

    public bool CanMove => true;
    public FileType FileType => FileType.RegularFile;
    public bool IsReadable => true;
    public bool IsWritable { get; init; } = true;
    public ulong AccessTime { get; set; }
    public ulong ModificationTime { get; set; }
    public ulong ChangeTime { get; set; }

    public InMemoryFile(ulong time, ReadOnlySpan<byte> content, MemoryStream? backing = null)
    {
        _memory = backing ?? new MemoryStream();
        _memory.Write(content);

        AccessTime = ModificationTime = ChangeTime = time;
    }

    IFileHandle IFile.Open(FdFlags flags)
    {
        return new Handle(this, flags);
    }

    public void MoveTo(Stream stream, ulong timestamp)
    {
        AccessTime = timestamp;
        _memory.Seek(0, SeekOrigin.Begin);
        _memory.WriteTo(stream);
    }

    public IFilesystemEntry ToInMemory()
    {
        return this;
    }
}