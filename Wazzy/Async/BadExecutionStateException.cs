namespace Wazzy.Async;

public class BadExecutionStateException
    : Exception
{
    public int ExecutionState { get; }
    public string MethodName { get; }

    public BadExecutionStateException(int executionState, string name)
        : base($"Bad execution state in '{name}': {executionState}")
    {
        ExecutionState = executionState;
        MethodName = name;
    }
}