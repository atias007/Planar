using CommonJob;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;

namespace Planar;

[DisallowConcurrentExecution]
[PersistJobDataAfterExecution]
internal class WorkflowJobNoConcurrent(
    ILogger<WorkflowJobNoConcurrent> logger,
    IJobPropertyDataLayer dataLayer,
    JobMonitorUtil jobMonitorUtil,
    IValidator<WorkflowJobProperties> validator) : WorkflowJob(logger, dataLayer, jobMonitorUtil, validator)
{
}