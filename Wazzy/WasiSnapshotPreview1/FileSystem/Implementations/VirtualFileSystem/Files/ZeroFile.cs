namespace Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Files;

public class ZeroFile
    : IFile
{
    private class Handle
        : BaseFileHandle<ZeroFile>
    {
        public Handle(ZeroFile file, FdFlags flags)
            : base(file, flags)
        {
        }

        public override ulong Position
        {
            get => 0;
            set { }
        }

        public override ulong Size => 0;

        public override void Dispose()
        {
        }

        public override Task<uint> Read(Memory<byte> bytes, ulong timestamp)
        {
            TryRead(timestamp);

            bytes.Span.Clear();
            return Task.FromResult((uint)bytes.Length);
        }

        public override void Truncate(ulong timestamp, long size)
        {
            TryWrite(timestamp);
        }

        public override uint Write(ReadOnlySpan<byte> bytes, ulong timestamp)
        {
            TryWrite(timestamp);
            return (uint)bytes.Length;
        }

        private void TryRead(ulong timestamp)
        {
            File.AccessTime = timestamp;
        }

        private void TryWrite(ulong timestamp)
        {
            File.AccessTime = timestamp;
            File.ModificationTime = timestamp;
            File.ChangeTime = timestamp;
        }

        public override ulong PollReadableBytes()
        {
            return ushort.MaxValue;
        }

        public override ulong PollWritableBytes()
        {
            return ushort.MaxValue;
        }
    }

    public bool CanMove => true;
    public FileType FileType => FileType.RegularFile;
    public ulong AccessTime { get; set; }
    public ulong ModificationTime { get; set; }
    public ulong ChangeTime { get; set; }
    public bool IsReadable => true;
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
        return this;
    }
}