using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;

namespace Planar;

[PersistJobDataAfterExecution]
public class WorkflowJobConcurrent(
    ILogger<WorkflowJobConcurrent> logger,
    IJobPropertyDataLayer dataLayer,
    JobMonitorUtil jobMonitorUtil) : WorkflowJob(logger, dataLayer, jobMonitorUtil)
{
}