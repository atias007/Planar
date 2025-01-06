using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;

namespace Planar;

[DisallowConcurrentExecution]
[PersistJobDataAfterExecution]
internal class WorkflowJobNoConcurrent(
    ILogger<WorkflowJobNoConcurrent> logger,
    IJobPropertyDataLayer dataLayer,
    JobMonitorUtil jobMonitorUtil) : WorkflowJob(logger, dataLayer, jobMonitorUtil)
{
}