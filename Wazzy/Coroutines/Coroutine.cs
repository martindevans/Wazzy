using System.Collections;
using System.Runtime.ExceptionServices;

namespace Wazzy.Coroutines;

/// <summary>
/// A coroutine for single threaded asynchronous computation.
/// - yield `null` to yield
/// - yield `IEnuemrator` to execute a nested coroutine
/// - yield `T` to immediately complete the coroutine
/// </summary>
/// <typeparam name="T"></typeparam>
internal class Coroutine<T>
{
    private readonly Stack<IEnumerator> _enumerators = [ ];

    private ExceptionDispatchInfo? _exception;

    private bool _hasResult;
    private T? _result;

    /// <summary>
    /// Check if this future has completed running and currently has a result available.
    /// Call `TryComplete` once this becomes true to retrieve the final value.
    /// </summary>
    public bool HasResult => _hasResult || _exception != null;

    /// <summary>
    /// Check if this coroutine will throw when TryGetResult is called
    /// </summary>
    public bool HasExceptionResult => _exception != null;

    public Coroutine(IEnumerator enumerator)
    {
        _enumerators.Push(enumerator);
    }

    /// <summary>
    /// Resume execution of this Future. If execution finishes the `IsCompleted` will become true.
    /// </summary>
    /// <returns>true, if `Resume` needs to be called again.</returns>
    public bool Resume()
    {
        if (HasResult)
            return false;

        try
        {
            if (_enumerators.Count > 0)
            {
                var e = _enumerators.Peek();
                if (!e.MoveNext())
                {
                    _enumerators.Pop();
                }
                else
                {
                    var item = e.Current;

                    switch (item)
                    {
                        case IEnumerator inner:
                            _enumerators.Push(inner);
                            break;

                        case T t:
                            _hasResult = true;
                            _result = t;
                            break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _exception = ExceptionDispatchInfo.Capture(ex);
            return false;
        }

        if (_enumerators.Count == 0 && !_hasResult)
            throw new InvalidOperationException("Reached end of coroutine without a result");

        return _enumerators.Count > 0 && !_hasResult;
    }

    /// <summary>
    /// Get a result from this future if it is completed. This can only be called once for any given future!
    /// </summary>
    /// <param name="result">The result of the call.</param>
    /// <returns>true if a result was produced, false if no result is available yet</returns>
    public bool TryGetResult(out T? result)
    {
        _exception?.Throw();

        if (_hasResult)
        {
            result = _result!;
            return true;
        }

        result = default;
        return false;
    }
}