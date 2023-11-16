using Wasmtime;

namespace Wazzy.Extensions;

internal static class MemoryExtensions
{
    private static void GrowToByteSize(this Memory memory, long finalBytesSize)
    {
        var currentBytesSize = memory.GetLength();
        var deltaBytes = finalBytesSize - currentBytesSize;

        if (deltaBytes <= 0)
            return;

        var pageSize = currentBytesSize / memory.GetSize();
        var deltaPages = (long)Math.Ceiling(deltaBytes / (double)pageSize);
        memory.Grow(deltaPages);
    }

    /// <summary>
    /// copy from memory (starting at zero) into the given span
    /// </summary>
    /// <param name="memory"></param>
    /// <param name="dest"></param>
    internal static void ReadMemory(this Memory memory, Span<byte> dest)
    {
        GrowToByteSize(memory, dest.Length);
        memory.GetSpan(0, dest.Length).CopyTo(dest);
    }

    /// <summary>
    /// copy from the given span into memory (starting at zero)
    /// </summary>
    /// <param name="memory"></param>
    /// <param name="src"></param>
    internal static void WriteMemory(this Memory memory, ReadOnlySpan<byte> src)
    {
        GrowToByteSize(memory, src.Length);
        src.CopyTo(memory.GetSpan(0, src.Length));
    }
}