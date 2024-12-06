using System.Buffers;
using System.Text;

namespace Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Files;

/// <summary>
/// File which writes everything written to Console
/// </summary>
public class ConsoleLog
    : IFile
{
    private class Handle
        : BaseFileHandle<ConsoleLog>
    {
        private readonly List<byte> _builder = [];

        public override ulong Position
        {
            get => 0;
            set { }
        }

        public override ulong Size => 0;

        public Handle(ConsoleLog log, FdFlags flags)
            : base(log, flags)
        {
        }

        public override void Dispose()
        {
            Sync();
        }

        public override Task<uint> Read(Memory<byte> memory, ulong timestamp)
        {
            throw new NotSupportedException("Cannot read ConsoleLog");
        }

        public override void Truncate(ulong timestamp, long size)
        {
            throw new NotSupportedException("Cannot truncate ConsoleLog");
        }

        public override uint Write(ReadOnlySpan<byte> bytes, ulong timestamp)
        {
            File.AccessTime = timestamp;
            File.ModificationTime = timestamp;
            File.ChangeTime = timestamp;

            _builder.AddRange(bytes);

            if (_builder.Count >= File._maxBufferSize)
            {
                WriteToLog(_builder.Count);
            }
            else if (File._newlineFlush)
            {
                // Keep writing to out while there's something in the buffer terminated with a newline
                while (true)
                {
                    var newline = _builder.IndexOf((byte)'\n');
                    if (newline != -1)
                        WriteToLog(newline + 1);
                    else
                        break;
                }
            }

            return (uint)bytes.Length;
        }

        private void WriteToLog(int count)
        {
            if (count == 0)
                return;

            var arr = ArrayPool<byte>.Shared.Rent(count);
            try
            {
                // Copy relevant slice into array
                Array.Clear(arr, 0, arr.Length);
                _builder.CopyTo(0, arr, 0, count);
                _builder.RemoveRange(0, count);

                // Format message
                var str = Encoding.UTF8.GetString(arr, 0, count).ReplaceLineEndings();
                var msg = str;
                if (!string.IsNullOrEmpty(File._prefix))
                    msg = $"[{File._prefix}]: {str}";

                var prevc = Console.ForegroundColor;
                Console.ForegroundColor = File._color ?? prevc;
                {
                    if (File._error)
                    {
                        Console.Error.Write(msg);
                        Console.Error.Flush();
                    }
                    else
                    {
                        Console.Write(msg);
                        Console.Out.Flush();
                    }
                }
                Console.ForegroundColor = prevc;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(arr);
            }
        }

        public override void Sync()
        {
            WriteToLog(_builder.Count);
            base.Sync();
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

    private readonly string _prefix;
    private readonly ConsoleColor? _color;
    private readonly bool _error;
    private readonly int _maxBufferSize;
    private readonly bool _newlineFlush;

    public bool CanMove => true;

    public FileType FileType => FileType.CharacterDevice;

    public ConsoleLog(string prefix, ConsoleColor? color = null, bool error = false, int maxBufferSize = 1024, bool newlineFlush = true)
    {
        _prefix = prefix;
        _color = color;
        _error = error;
        _maxBufferSize = maxBufferSize;
        _newlineFlush = newlineFlush;
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