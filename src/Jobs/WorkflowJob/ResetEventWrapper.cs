using CommonJob;
using Quartz;

namespace Planar;

internal sealed class ResetEventWrapper
{
    private ResetEventWrapper(JobKey jobKey, WorkflowJobStep step)
    {
        JobKey = jobKey;
        Timeout = step.Timeout;
    }

    public AutoResetEvent ResetEvent { get; private set; } = new AutoResetEvent(false);
    public JobKey JobKey { get; private set; }
    public TimeSpan? Timeout { get; private set; }
    public WorkflowJobStepEvent? Event { get; set; }

    public static ResetEventWrapper Create(JobKey jobKey, WorkflowJobStep step) => new(jobKey, step);
}