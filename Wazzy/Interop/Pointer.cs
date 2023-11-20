using System.Diagnostics;
using System.Runtime.InteropServices;
using Wasmtime;

namespace Wazzy.Interop;

/// <summary>
/// Represents a pointer into a specific location in memory
/// </summary>
/// <typeparam name="T">Type of item pointed to</typeparam>
[StructLayout(LayoutKind.Sequential)]
[DebuggerDisplay("Pointer<{typeof(T).Name,nq}>({Addr})")]
public readonly struct Pointer<T>
    where T : unmanaged
{
    /// <summary>
    /// Raw address
    /// </summary>
    public readonly int Addr;

    /// <summary>
    /// Create a new pointer
    /// </summary>
    /// <param name="addr">The pointer value</param>
    public Pointer(int addr)
    {
        Addr = addr;
    }

    /// <summary>
    /// Convert into a reference into the given memory
    /// </summary>
    /// <param name="memory">The memory to point to</param>
    /// <returns>A reference to an item within the given memory</returns>
    public ref T Deref(Memory memory)
    {
        return ref memory.GetSpan<T>(Addr, 1)[0];
    }

    /// <summary>
    /// Convert into a reference into the given memory
    /// </summary>
    /// <param name="caller">The caller to retrieve the memory to point to</param>
    /// <returns>A reference to an item within the memory retrieved from the caller</returns>
    public ref T Deref(Caller caller)
    {
        return ref Deref(caller.GetMemory("memory")!);
    }
}