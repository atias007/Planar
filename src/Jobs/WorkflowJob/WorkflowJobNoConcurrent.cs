using CommonJob;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.General;
using Quartz;

namespace Planar;

[DisallowConcurrentExecution]
[PersistJobDataAfterExecution]
internal class WorkflowJobNoConcurrent(
    ILogger<WorkflowJobNoConcurrent> logger,
    IJobPropertyDataLayer dataLayer,
    JobMonitorUtil jobMonitorUtil,
    IClusterUtil clusterUtil,
    IValidator<WorkflowJobProperties> validator) : WorkflowJob(logger, dataLayer, jobMonitorUtil, clusterUtil, validator)
{
}