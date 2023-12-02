using System.Collections.Concurrent;

namespace Wazzy.Async;

internal class SavedStackData
{
    private const int MaxPoolSize = 16;
    private static readonly ConcurrentBag<SavedStackData> _pool = new();

    public byte[] Data { get; set; }
    public int Epoch { get; private set; }

    private SavedStackData()
    {
        Data = new byte[WasmAsyncExtensions.StashSize];
        Epoch = 0;
    }

    public static SavedStackData Get()
    {
        if (_pool.TryTake(out var result))
            return result;

        return new SavedStackData();
    }

    public static void Return(SavedStackData stack)
    {
        stack.Epoch++;

        if (_pool.Count < MaxPoolSize)
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

    private readonly SavedStackData _data;
    private readonly int _epoch;

    internal ReadOnlySpan<byte> Value
    {
        get
        {
            CheckEpoch();
            return _data.Data;
        }
    }

    internal bool IsNull => _data == null;

    /// <summary>
    /// Represents a stack that has been rewound out of a WASM Instance and may be resumed.
    /// </summary>
    internal SavedStack(SavedStackData value)
    {
        _data = value;
        _epoch = _data.Epoch;
    }

    /// <summary>
    /// Dispose this stack, returning memory to the pool
    /// </summary>
    internal void Dispose()
    {
        if (_epoch == _data.Epoch)
            SavedStackData.Return(_data);
    }

    private void CheckEpoch()
    {
        if (_epoch != _data.Epoch)
            throw new ObjectDisposedException("Cannot access saved stack after it has been used once");
    }
}