using System.Collections.Concurrent;

namespace Wazzy.Async;

internal class SavedStackData
{
    private const int MaxPoolSize = 16;
    private static readonly ConcurrentBag<SavedStackData> _pool = new();

    public byte[] Data { get; set; }
    public int LocalsSize { get; set; }

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
    : IDisposable
{
    private readonly SavedStackData _data;
    private readonly int _epoch;

    public ReadOnlySpan<byte> Value
    {
        get
        {
            CheckEpoch();
            return _data.Data;
        }
    }

    public int LocalsSize
    {
        get
        {
            CheckEpoch();
            return _data.LocalsSize;
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
    public void Dispose()
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