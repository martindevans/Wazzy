using System.Runtime.CompilerServices;

namespace Wazzy.Async;

public class BadExecutionStateException(int executionState, string name)
    : Exception($"Bad execution state in '{name}': {executionState}")
{
    public int ExecutionState { get; } = executionState;
    public string MethodName { get; } = name;
}