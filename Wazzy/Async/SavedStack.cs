using System.Collections.Concurrent;

namespace Wazzy.Async;

internal class SavedStackData
{
    private const int MaxPoolSize = 16;
    private static readonly ConcurrentBag<SavedStackData> _pool = [];

    public int Epoch { get; private set; }

    public int? AllocatedBufferAddress { get; set; }
    public byte[] Data { get; }

    public object? Locals { get; set; }
    public int ExecutionState { get; set; }

    private SavedStackData()
    {
        Data = new byte[WasmAsyncExtensions.StashSize];
        Epoch = 0;
    }

    public static SavedStackData Get()
    {
        if (_pool.TryTake(out var result))
        {
            result.Epoch++;
            return result;
        }

        return new SavedStackData();
    }

    public static void Return(SavedStackData stack)
    {
        // Invalid all handles
        stack.Epoch++;

        // Reset
        stack.AllocatedBufferAddress = default;
        stack.Locals = null;
        stack.ExecutionState = 0;

        // Add this item to the pool if possible
        if (_pool.Count < MaxPoolSize && stack.Epoch < int.MaxValue - 100)
            _pool.Add(stack);
    }
}

/// <summary>
/// Represents a stack that has been rewound out of a WASM Instance and may be resumed.
/// </summary>
public readonly struct SavedStack
{
    // Implementation note, this is **NOT** actually the stack data! While the program is suspended the rewind stack is left in-place
    // in the WASM memory, ready to use for resuming. This actually contains the non-stack data that was saved from that location in memory.

    internal readonly SavedStackData Data;
    private readonly int _epoch;

    internal bool IsNull => Data == null;

    /// <summary>
    /// Represents a stack that has been rewound out of a WASM Instance and may be resumed.
    /// </summary>
    internal SavedStack(SavedStackData value)
    {
        Data = value;
        _epoch = Data.Epoch;
    }

    internal void CheckEpoch()
    {
        if (_epoch != Data.Epoch)
            throw new ObjectDisposedException("Cannot access saved stack after it has been used once");
    }
}