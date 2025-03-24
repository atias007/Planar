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
    private ResetEventWrapper(JobKey jobKey, string sequenceFireInstanceId, JobDataMap dataMap, SequenceJobStep step)
    {
        ResetEvent = new AutoResetEvent(false);
        JobKey = jobKey;
        Timeout = step.Timeout;
        DataMap = dataMap;
        SequenceFireInstanceId = sequenceFireInstanceId;
        Key = GetKey(jobKey, SequenceFireInstanceId);
    }

    public AutoResetEvent ResetEvent { get; private set; }
    public JobKey JobKey { get; private set; }
    public TimeSpan? Timeout { get; private set; }
    public JobDataMap DataMap { get; private set; }
    public string SequenceFireInstanceId { get; private set; }
    public string Key { get; private set; }
    public StepStatus Status { get; private set; }
    public SequenceJobStepEvent Event { get; set; } = SequenceJobStepEvent.Unknown;
    public string? FireInstanceId { get; set; }
    public string DisplayStatus => Status == StepStatus.Finish ? Event.ToString() : Status.ToString();

    public void SetStatus(StepStatus status) => Status = status;

    public static ResetEventWrapper Create(JobKey jobKey, string sequenceFireInstanceId, JobDataMap dataMap, SequenceJobStep step) =>
        new(jobKey, sequenceFireInstanceId, dataMap, step);

    public static string GetKey(JobKey stepJobKey, string sequenceFireInstanceId)
    {
        var key = $"{stepJobKey}~~~{sequenceFireInstanceId}";
        return key;
    }
}