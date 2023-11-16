namespace Wazzy.Async;

/// <summary>
/// Indicates the current async state of a WASM Instance.
/// </summary>
public enum AsyncState
{
    /// <summary>
    /// No async state, normal execution.
    /// </summary>
    None = 0,

    /// <summary>
    /// WASM stack is currently in the process of being suspended
    /// </summary>
    Suspending = 1,

    /// <summary>
    /// WASM stack is currently in the process of resuming from a previous suspension
    /// </summary>
    Resuming = 2
}

internal static class AsyncStateExtensions
{
    public static void AssertState(this AsyncState? actual, AsyncState expected)
    {
        if (expected != actual)
            throw new InvalidOperationException($"Incorrect WASM Async State!. Expected:{expected}, Actual:{actual?.ToString() ?? "null"}");
    }
}