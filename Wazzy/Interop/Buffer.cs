using System.Runtime.InteropServices;
using Wasmtime;

namespace Wazzy.Interop;

/// <summary>
/// Represents a fixed size buffer of memory
/// </summary>
/// <typeparam name="T">Type of items within this buffer</typeparam>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Buffer<T>
    where T : unmanaged
{
    /// <summary>
    /// Raw address of the buffer start
    /// </summary>
    public readonly int Addr;

    /// <summary>
    /// Number of elements in the buffer
    /// </summary>
    public readonly uint Length;

    /// <summary>
    /// Create a new buffer
    /// </summary>
    /// <param name="addr">Address of the buffer start</param>
    /// <param name="length">Number of elements in the buffer</param>
    public Buffer(int addr, uint length)
    {
        Addr = addr;
        Length = length;
    }

    /// <summary>
    /// Get a span which points to this buffer in the given memory space
    /// </summary>
    /// <param name="memory">Memory to point to</param>
    /// <returns>A span, pointing to this buffer within the given memory</returns>
    public Span<T> GetSpan(Memory memory)
    {
        return memory.GetSpan<T>(Addr, (int)Length);
    }

    /// <summary>
    /// Get a span which points to this buffer in the given memory space
    /// </summary>
    /// <param name="caller">Caller to use to get the Memory from</param>
    /// <returns>A span, pointing to this buffer within the memory retrieved from the Caller</returns>
    public Span<T> GetSpan(Caller caller)
    {
        return GetSpan(caller.GetMemory("memory")!);
    }

    public override string ToString()
    {
        return $"Buffer<{nameof(T)}>({Addr},{Length})";
    }
}