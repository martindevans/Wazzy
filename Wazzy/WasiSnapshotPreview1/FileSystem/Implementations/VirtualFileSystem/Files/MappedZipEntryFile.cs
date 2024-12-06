using System.IO.Compression;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Directories;

namespace Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Files;

public class MappedZipEntryFile
        : IFile
{
    private class Handle
        : BaseFileHandle<MappedZipEntryFile>
    {
        private readonly Stream _stream;

        private long _position;

        public Handle(MappedZipEntryFile file, FdFlags flags)
            : base(file, flags)
        {
            _stream = new MemoryStream(file.GetDecompressedContent());
        }

        public override void Dispose()
        {
            _stream.Dispose();
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

        public override ulong Size => (ulong)_stream.Length;

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

        public override async Task<uint> Read(Memory<byte> bytes, ulong timestamp)
        {
            File.AccessTime = timestamp;

            _stream.Seek((long)Position, SeekOrigin.Begin);
            var read = await _stream.ReadAsync(bytes);
            Position += (ulong)read;

            return (uint)read;
        }

        public override void Truncate(ulong timestamp, long size)
        {
            throw new NotSupportedException();
        }
    }

    public MappedZipEntryFile(MappedZipArchiveDirectoryContent archive, string fullPath)
    {
        _entry = archive.GetEntry(fullPath) ?? throw new InvalidOperationException("No such ZipArchive entry");
    }

    public bool CanMove => false;
    public FileType FileType => FileType.RegularFile;
    public ulong AccessTime { get; set; }
    public ulong ModificationTime { get; set; }
    public ulong ChangeTime { get; set; }

    private readonly ZipArchiveEntry _entry;
    private byte[]? _decompressedCache;

    private byte[] GetDecompressedContent()
    {
        if (_decompressedCache == null)
        {
            using var stream = _entry.Open();
            _decompressedCache = new byte[_entry.Length];
            var output = new MemoryStream(_decompressedCache);
            stream.CopyTo(output);
        }

        return _decompressedCache;
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