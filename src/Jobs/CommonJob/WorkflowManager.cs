using Quartz;
using System.Collections.Concurrent;

namespace CommonJob;

public interface IWorkflowInstance
{
    void SignalEvent(JobKey jobKey, WorkflowJobStepEvent @event);
}

public static class WorkflowManager
{
    private static readonly ConcurrentDictionary<string, IWorkflowInstance> _workflows = [];

    public static void RegisterWorkflow(string key, IWorkflowInstance workflow)
    {
        _workflows.TryAdd(key, workflow);
    }

    public static void SignalEvent(string instanceId, JobKey jobKey, WorkflowJobStepEvent @event)
    {
        if (_workflows.TryGetValue(instanceId, out var workflow))
        {
            workflow.SignalEvent(jobKey, @event);
        }
    }

    public static void UnregisterWorkflow(string key)
    {
        _workflows.TryRemove(key, out _);
    }
}