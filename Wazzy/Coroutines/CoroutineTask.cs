using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace Wazzy.Coroutines;

internal interface ICoroutineTask
{
    bool IsCompleted { get; }

    void Resume();
}

[AsyncMethodBuilder(typeof(CoroutineBuilder<>))]
internal class CoroutineTask<T>
    : ICoroutineTask
{
    private readonly IAsyncStateMachine _stateMachine;

    private ICoroutineTaskAwaiter? _innerCoroutine;
    internal bool Awaiting { get; set; }

    private ExceptionDispatchInfo? _exception;
    private bool _hasResult;
    private T? _result;

    public bool IsCompleted => _hasResult || _exception != null;
    public bool IsCompletedWithException => IsCompleted && _exception != null;
    public bool IsCompletedWithResult => IsCompleted && _hasResult;

    internal CoroutineTask(IAsyncStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
    }

    public CoroutineTaskAwaiter<T> GetAwaiter()
    {
        return new CoroutineTaskAwaiter<T>(this);
    }

    public void Resume()
    {
        // Check if the task has completed
        if (_hasResult)
            return;
        if (_exception != null)
            return;

        // If there's an inner coroutine, pump that
        if (_innerCoroutine != null)
        {
            _innerCoroutine.Task.Resume();
            if (_innerCoroutine.Task.IsCompleted)
                _innerCoroutine = null;
            return;
        }

        // Check if we're waiting for some other task to complete. If so we don't want
        // to advance the state machine, because it would block on that task!
        if (Awaiting)
            return;

        _stateMachine.MoveNext();
    }

    /// <summary>
    /// Get a result from this future if it is completed. This can only be called once for any given future!
    /// </summary>
    /// <param name="result">The result of the call.</param>
    /// <returns>true if a result was produced, false if no result is available yet</returns>
    public bool TryGetResult([NotNullWhen(true)] out T? result)
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

    internal void SetException(Exception exception)
    {
        _exception = ExceptionDispatchInfo.Capture(exception);
    }

    internal void SetResult(T result)
    {
        _hasResult = true;
        _result = result;
    }

    public void SetInnerCoroutine(ICoroutineTaskAwaiter coro)
    {
        if (_innerCoroutine != null)
            throw new InvalidOperationException("Cannot set an inner coroutine while one is already set");
        _innerCoroutine = coro;
    }
}

internal interface ICoroutineTaskAwaiter
{
    ICoroutineTask Task { get; }
}

internal class CoroutineTaskAwaiter<T>(ICoroutineTask task)
    : INotifyCompletion, ICoroutineTaskAwaiter
{
    public ICoroutineTask Task { get; } = task;

    public bool IsCompleted => Task.IsCompleted;

    public T GetResult()
    {
        if (!((CoroutineTask<T>)Task).TryGetResult(out var result))
            throw new InvalidOperationException("Cannot get result from coroutine task before it is completed");

        return result!;
    }

    public void OnCompleted(Action completion)
    {
        throw new NotSupportedException();
    }
}

internal class CoroutineBuilder<T>
{
    private IAsyncStateMachine _stateMachine = null!;

    public CoroutineTask<T> Task { get; private set; } = null!;

    public static CoroutineBuilder<T> Create()
    {
        return new CoroutineBuilder<T>();
    }

    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
    {
        _stateMachine = stateMachine;
        Task = new CoroutineTask<T>(stateMachine);
    }

    public void SetStateMachine(IAsyncStateMachine stateMachine)
    {
        throw new NotSupportedException("Never called, but required by the compiler");
    }

    public void SetException(Exception exception)
    {
        Task.SetException(exception);
    }

    public void SetResult(T result)
    {
        Task.SetResult(result);
    }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        if (awaiter is ICoroutineTaskAwaiter coro)
        {
            Task.SetInnerCoroutine(coro);
        }
        else
        {
            Task.Awaiting = true;
            awaiter.OnCompleted(() =>
            {
                Task.Awaiting = false;
                _stateMachine.MoveNext();
            });
        }
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        AwaitOnCompleted(ref awaiter, ref stateMachine);
    }
}