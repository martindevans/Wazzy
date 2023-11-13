using System.Runtime.InteropServices;
using Wasmtime;

namespace Wazzy.Interop;

/// <summary>
/// Represents a pointer into a specific location in memory which can only be read from
/// </summary>
/// <typeparam name="T">Type of item pointed to</typeparam>
[StructLayout(LayoutKind.Sequential)]
public readonly struct ReadonlyPointer<T>
    where T : unmanaged
{
    /// <summary>
    /// Raw address
    /// </summary>
    public readonly int Addr;

    /// <summary>
    /// Create a new pointer
    /// </summary>
    /// <param name="addr">pointer value</param>
    public ReadonlyPointer(int addr)
    {
        Addr = addr;
    }

    /// <summary>
    /// Convert a pointer into a readonly pointer
    /// </summary>
    /// <param name="ptr">Pointer to convert</param>
    public static implicit operator ReadonlyPointer<T>(Pointer<T> ptr)
    {
        return new ReadonlyPointer<T>(ptr.Addr);
    }

    /// <summary>
    /// Convert into a reference into the given memory
    /// </summary>
    /// <param name="memory">The memory to point to</param>
    /// <returns>A reference to an item within the given memory</returns>
    public ref readonly T Deref(Memory memory)
    {
        return ref new Pointer<T>(Addr).Deref(memory);
    }

    /// <summary>
    /// Convert into a reference into the given memory
    /// </summary>
    /// <param name="caller">The caller to retrieve the memory to point to</param>
    /// <returns>A reference to an item within the memory retrieved from the caller</returns>
    public ref readonly T Deref(Caller caller)
    {
        return ref new Pointer<T>(Addr).Deref(caller);
    }

    public override string ToString()
    {
        return $"ReadonlyPointer<{typeof(T).Name}>({Addr})";
    }
}