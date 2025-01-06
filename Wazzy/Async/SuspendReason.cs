namespace Wazzy.Async;

public interface IAsyncifySuspendReason
{
}

/// <summary>
/// No reason was specified for the async suspend
/// </summary>
public sealed class UnspecifiedSuspend
    : IAsyncifySuspendReason
{
    /// <summary>
    /// Get the singleton instance of this class
    /// </summary>
    public static UnspecifiedSuspend Instance => new();

    private UnspecifiedSuspend()
    {
    }
}

/// <summary>
/// Suspend was caused by an explicit call to sched_yield
/// </summary>
public sealed class SchedYieldSuspend
    : IAsyncifySuspendReason
{
    /// <summary>
    /// Get the singleton instance of this class
    /// </summary>
    public static SchedYieldSuspend Instance => new();

    private SchedYieldSuspend()
    {
    }
}

/// <summary>
/// Suspend was caused by waiting for a <see cref="Task"/> to complete
/// </summary>
public class TaskSuspend
    : IAsyncifySuspendReason
{
    public Task Task { get; }

    public TaskSuspend(Task task)
    {
        Task = task;
    }

    public static IAsyncifySuspendReason Create(Task? task)
    {
        return task != null
             ? new TaskSuspend(task)
             : UnspecifiedSuspend.Instance;
    }
}