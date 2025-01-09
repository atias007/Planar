using CommonJob;
using Quartz;

namespace Planar;

internal sealed class ResetEventWrapper
{
    private ResetEventWrapper(JobKey jobKey, JobDataMap dataMap, WorkflowJobStep step)
    {
        JobKey = jobKey;
        Timeout = step.Timeout;
        DataMap = dataMap;
    }

    public AutoResetEvent ResetEvent { get; private set; } = new AutoResetEvent(false);
    public JobKey JobKey { get; private set; }
    public TimeSpan? Timeout { get; private set; }
    public JobDataMap DataMap { get; private set; }
    public bool Done { get; private set; }
    public WorkflowJobStepEvent? Event { get; set; }

    public void SetDone() => Done = true;

    public static ResetEventWrapper Create(JobKey jobKey, JobDataMap dataMap, WorkflowJobStep step) =>
        new(jobKey, dataMap, step);
}