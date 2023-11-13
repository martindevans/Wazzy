using System.Runtime.InteropServices;
using Wasmtime;

namespace Wazzy.Interop;

/// <summary>
/// Represents a fixed size buffer of memory which can only be read from
/// </summary>
/// <typeparam name="T">Type of elements</typeparam>
[StructLayout(LayoutKind.Sequential)]
public readonly struct ReadonlyBuffer<T>
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
    public ReadonlyBuffer(int addr, uint length)
    {
        Addr = addr;
        Length = length;
    }

    /// <summary>
    /// Convert a buffer into a readonly buffer
    /// </summary>
    /// <param name="buf">Buffer to convert</param>
    public static implicit operator ReadonlyBuffer<T>(Buffer<T> buf)
    {
        return new ReadonlyBuffer<T>(buf.Addr, buf.Length);
    }

    public ReadOnlySpan<T> GetSpan(Memory memory)
    {
        return new Buffer<T>(Addr, Length).GetSpan(memory);
    }

    public ReadOnlySpan<T> GetSpan(Caller caller)
    {
        return new Buffer<T>(Addr, Length).GetSpan(caller);
    }

    public override string ToString()
    {
        return $"ReadonlyBuffer<{nameof(T)}>({Addr},{Length})";
    }
}