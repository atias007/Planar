using Quartz;
using System.Collections.Concurrent;

namespace CommonJob;

public interface IWorkflowInstance
{
    void SignalEvent(JobKey stepJobKey, string workflowFireInstanceId, WorkflowJobStepEvent @event);
}

public static class WorkflowManager
{
    private static readonly ConcurrentDictionary<string, IWorkflowInstance> _workflows = [];

    public static void RegisterWorkflow(string instanceId, IWorkflowInstance workflow)
    {
        _workflows.TryAdd(instanceId, workflow);
    }

    public static void SignalEvent(JobKey stepJobKey, string workflowFireInstanceId, WorkflowJobStepEvent @event)
    {
        if (_workflows.TryGetValue(workflowFireInstanceId, out var workflow))
        {
            workflow.SignalEvent(stepJobKey, workflowFireInstanceId, @event);
        }
    }

    public static void UnregisterWorkflow(string instanceId)
    {
        _workflows.TryRemove(instanceId, out _);
    }
}