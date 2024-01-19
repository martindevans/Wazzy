using Wasmtime;

namespace Wazzy.Extensions;

internal static class MemoryExtensions
{
    private static void GrowToByteSize(this Memory memory, long finalBytesSize)
    {
        // Ensure memory has at least 1 page!
        var currentBytesSize = memory.GetLength();
        if (currentBytesSize == 0)
        {
            memory.Grow(1);
            currentBytesSize = memory.GetLength();
        }

        // How many bytes do we need to grow by?
        var deltaBytes = finalBytesSize - currentBytesSize;

        // Easy early exit if we don't actually need any more
        if (deltaBytes <= 0)
            return;

        // And how many pages are needed
        var deltaPages = (long)Math.Ceiling(deltaBytes / (double)Memory.PageSize);

        // Grow
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