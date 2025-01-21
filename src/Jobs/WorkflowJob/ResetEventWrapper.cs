using CommonJob;
using Quartz;

namespace Planar;

public enum StepStatus
{
    Waiting,
    Start,
    Interrupted,
    Finish
}

internal sealed class ResetEventWrapper
{
    private ResetEventWrapper(JobKey jobKey, string workflowFireInstanceId, JobDataMap dataMap, WorkflowJobStep step)
    {
        ResetEvent = new AutoResetEvent(false);
        JobKey = jobKey;
        Timeout = step.Timeout;
        DataMap = dataMap;
        WorkflowFireInstanceId = workflowFireInstanceId;
        Key = GetKey(jobKey, WorkflowFireInstanceId);
    }

    public AutoResetEvent ResetEvent { get; private set; }
    public JobKey JobKey { get; private set; }
    public TimeSpan? Timeout { get; private set; }
    public JobDataMap DataMap { get; private set; }
    public string WorkflowFireInstanceId { get; private set; }
    public string Key { get; private set; }
    public StepStatus Status { get; private set; }
    public WorkflowJobStepEvent Event { get; set; } = WorkflowJobStepEvent.Unknown;
    public string? FireInstanceId { get; set; }
    public string DisplayStatus => Status == StepStatus.Finish ? Event.ToString() : Status.ToString();

    public void SetStatus(StepStatus status) => Status = status;

    public static ResetEventWrapper Create(JobKey jobKey, string workflowFireInstanceId, JobDataMap dataMap, WorkflowJobStep step) =>
        new(jobKey, workflowFireInstanceId, dataMap, step);

    public static string GetKey(JobKey stepJobKey, string workflowFireInstanceId)
    {
        var key = $"{stepJobKey}~~~{workflowFireInstanceId}";
        return key;
    }
}