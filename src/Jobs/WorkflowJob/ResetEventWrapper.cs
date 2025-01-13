using CommonJob;
using Quartz;

namespace Planar;

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
    public bool Done { get; private set; }
    public WorkflowJobStepEvent? Event { get; set; }

    public void SetDone() => Done = true;

    public static ResetEventWrapper Create(JobKey jobKey, string workflowFireInstanceId, JobDataMap dataMap, WorkflowJobStep step) =>
        new(jobKey, workflowFireInstanceId, dataMap, step);

    public static string GetKey(JobKey stepJobKey, string workflowFireInstanceId)
    {
        var key = $"{stepJobKey}~~~{workflowFireInstanceId}";
        return key;
    }
}