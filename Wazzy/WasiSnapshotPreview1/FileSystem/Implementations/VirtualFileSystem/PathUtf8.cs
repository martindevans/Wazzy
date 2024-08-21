using System.Text;
using Wazzy.Extensions;

namespace Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem;

internal readonly ref struct PathUtf8(ReadOnlySpan<byte> bytes)
{
    public ReadOnlySpan<byte> Bytes { get; } = bytes;
    public int Length => Bytes.Length;

    public string String => Encoding.UTF8.GetString(Bytes);

    public bool IsAbsolute()
    {
        return Bytes.Length > 0 && Bytes[0] == (byte)'/';
    }

    public bool IsComplex()
    {
        var remaining = Bytes;

        do
        {
            remaining.Split((byte)'/', out var segment, out remaining);

            if (segment.Length != 1 && segment.Length != 2)
                continue;

            var count = 0;
            for (var i = 0; i < segment.Length; i++)
            {
                if (segment[i] == (byte)'.')
                    count++;
                else
                    break;
            }

            if (count == segment.Length)
                return true;
        } while (remaining.Length > 0);

        return false;
    }

    public void Split(byte splitchar, out PathUtf8 left, out PathUtf8 right)
    {
        Bytes.Split(splitchar, out var l, out var r);
        left = new PathUtf8(l);
        right = new PathUtf8(r);
    }

    /// <summary>
    /// Splits the path around the first `/`.
    /// </summary>
    public void Split(out PathUtf8 first, out PathUtf8 rest)
    {
        Split((byte)'/', out first, out rest);
    }

    /// <summary>
    /// Given a path get the segment after the last `/`.
    /// </summary>
    public ReadOnlySpan<byte> GetName()
    {
        Bytes.SplitLast((byte)'/', out var left, out var right);
        return right.Length == 0 ? left : right;
    }

    public override string ToString() => String;
}