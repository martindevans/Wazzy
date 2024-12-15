using System.IO.Compression;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Directories;

namespace Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Files;

public class MappedZipEntryFile
        : IFile
{
    private class Handle
        : BaseFileHandle<MappedZipEntryFile>
    {
        private readonly MappedZipEntryFile _file;

        private long _position;

        public Handle(MappedZipEntryFile file, FdFlags flags)
            : base(file, flags)
        {
            _file = file;
        }

        public override void Dispose()
        {
        }

        public override ulong Position
        {
            get => (ulong)_position;
            set
            {
                if (value > Size)
                    throw new InvalidOperationException("Position cannot be beyond the end of the file");

                _position = (long)value;
            }
        }

        public override ulong Size => _file.Size;

        public override uint Write(ReadOnlySpan<byte> bytes, ulong timestamp)
        {
            throw new NotSupportedException();
        }

        public override ulong PollReadableBytes()
        {
            return Size;
        }

        public override ulong PollWritableBytes()
        {
            return 0;
        }

        public override Task<uint> Read(Memory<byte> bytes, ulong timestamp)
        {
            File.AccessTime = timestamp;

            checked
            {
                var content = _file.GetDecompressedContent();
                var read = Math.Min(Size - Position, (ulong)bytes.Length);
                content.AsSpan(checked((int)Position), (int)read).CopyTo(bytes.Span);

                Position += read;

                return Task.FromResult((uint)read);
            }
        }

        public override void Truncate(ulong timestamp, long size)
        {
            throw new NotSupportedException();
        }
    }

    public MappedZipEntryFile(MappedZipArchiveDirectoryContent archive, string fullPath, bool contentCaching)
    {
        _contentCaching = contentCaching;
        _entry = archive.GetEntry(fullPath) ?? throw new InvalidOperationException("No such ZipArchive entry");
    }

    public bool CanMove => false;
    public FileType FileType => FileType.RegularFile;
    public ulong AccessTime { get; set; }
    public ulong ModificationTime { get; set; }
    public ulong ChangeTime { get; set; }

    private readonly bool _contentCaching;
    private readonly ZipArchiveEntry _entry;
    private byte[]? _decompressedCache;

    public ulong Size => checked((ulong)_entry.Length);

    private byte[] GetDecompressedContent()
    {
        var content = _decompressedCache ?? Decompress();

        if (_contentCaching)
            _decompressedCache = content;

        return content;
    }

    private byte[] Decompress()
    {
        using var stream = _entry.Open();
        var content = new byte[_entry.Length];
        var output = new MemoryStream(content);
        stream.CopyTo(output);

        return content;
    }

    public IFilesystemEntry ToInMemory()
    {
        throw new NotSupportedException();
    }

    public bool IsReadable => true;
    public bool IsWritable => false;

    public IFileHandle Open(FdFlags flags)
    {
        return new Handle(this, flags);
    }

    #region writing (not supported)
    public void MoveTo(Stream stream, ulong timestamp)
    {
        throw new NotSupportedException("Mapped ZipArchive file is not writable");
    }

    public void Delete()
    {
        throw new NotSupportedException("Mapped ZipArchive file is not writable");
    }
    #endregion
}