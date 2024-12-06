namespace Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Files;

internal class MappedFile
        : IFile
{
    private class Handle
        : BaseFileHandle<MappedFile>
    {
        private readonly Stream _stream;
        private readonly FileInfo _fileInfo;
        private readonly bool _flushWrites;

        public override ulong Position { get => (ulong)_stream.Position; set => _stream.Position = (long)value; }

        public override ulong Size
        {
            get
            {
                _fileInfo.Refresh();
                return (ulong)_fileInfo.Length;
            }
        }

        public Handle(MappedFile file, FdFlags flags) : base(file, flags)
        {
            var mode = (flags & FdFlags.Append) != 0 ? FileMode.Append : FileMode.Open;

            // todo check mapped file FileMode and FileShare options are correct

            _fileInfo = file._info;
            _stream = _fileInfo.Open(mode);
            _flushWrites = (flags & FdFlags.Sync) != 0 || (flags & FdFlags.DSync) != 0;
        }

        public override void Dispose()
        {
            _stream.Dispose();
        }

        public override async Task<uint> Read(Memory<byte> memory, ulong timestamp)
        {
            return (uint)await _stream.ReadAsync(memory);
        }

        public override void Truncate(ulong timestamp, long size)
        {
            if (size <= _stream.Length)
            {
                _stream.SetLength(size);
            }
            else
            {
                // Write out enough chunks to expand the file to at least the new size
                var delta = size - _stream.Length;
                Span<byte> zero = stackalloc byte[128];
                for (var i = 0; i < delta; i += 128)
                    _stream.Write(zero);

                // Truncate to the right size
                _stream.SetLength(size);
                _stream.Flush();
            }
        }

        public override uint Write(ReadOnlySpan<byte> bytes, ulong timestamp)
        {
            _stream.Write(bytes);
            if (_flushWrites)
                _stream.Flush();
            return (uint)bytes.Length;
        }

        public override void Sync()
        {
            _stream.Flush();
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

    private readonly FileInfo _info;
    private readonly IVFSClock _clock;
    private readonly bool _isReadonly;
    private readonly bool _isMounted;

    public bool CanMove => !_isMounted;
    public string HostPath => _info.FullName;
    public bool IsReadable => _info.Exists;
    public bool IsWritable => !_info.IsReadOnly && !_isReadonly;
    public FileType FileType => FileType.RegularFile;

    public ulong AccessTime
    {
        get => _clock.FromRealTime(new DateTimeOffset(_info.LastAccessTimeUtc));
        set => _info.LastAccessTimeUtc = _clock.ToRealTime(value).DateTime;
    }

    public ulong ModificationTime
    {
        get => _clock.FromRealTime(new DateTimeOffset(_info.LastWriteTimeUtc));
        set => _info.LastWriteTimeUtc = _clock.ToRealTime(value).DateTime;
    }

    public ulong ChangeTime
    {
        get => ModificationTime;
        set => ModificationTime = value;
    }


    public MappedFile(string path, IVFSClock clock, bool isReadonly, bool isMounted)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException(path);

        _info = new(path);
        _clock = clock;
        _isReadonly = isReadonly;
        _isMounted = isMounted;
    }

    public IFileHandle Open(FdFlags flags)
    {
        return new Handle(this, flags);
    }

    public void Delete()
    {
        if (!_isMounted)
            _info.Delete();
    }

    public void MoveTo(Stream stream, ulong timestamp)
    {
        if (_isMounted)
            throw new InvalidOperationException("Cannot move mounted host file");

        using var file = _info.OpenRead();
        file.CopyTo(stream);
        Delete();
    }

    public IFilesystemEntry ToInMemory()
    {
        if (_isMounted)
            throw new InvalidOperationException("Cannot move mounted host file");

        var bytes = File.ReadAllBytes(HostPath);
        var memoryFile = new InMemoryFile(_clock.GetTime(), bytes);
        Delete();
        return memoryFile;
    }
}