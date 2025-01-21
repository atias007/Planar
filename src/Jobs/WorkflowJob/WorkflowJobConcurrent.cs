using CommonJob;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.General;
using Quartz;

namespace Planar;

[PersistJobDataAfterExecution]
public class WorkflowJobConcurrent(
    ILogger<WorkflowJobConcurrent> logger,
    IJobPropertyDataLayer dataLayer,
    JobMonitorUtil jobMonitorUtil,
    IClusterUtil clusterUtil,
    IValidator<WorkflowJobProperties> validator) : WorkflowJob(logger, dataLayer, jobMonitorUtil, clusterUtil, validator)
{
}