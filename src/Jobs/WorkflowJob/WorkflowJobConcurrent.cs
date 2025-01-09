using CommonJob;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;

namespace Planar;

[PersistJobDataAfterExecution]
public class WorkflowJobConcurrent(
    ILogger<WorkflowJobConcurrent> logger,
    IJobPropertyDataLayer dataLayer,
    JobMonitorUtil jobMonitorUtil,
    IValidator<WorkflowJobProperties> validator) : WorkflowJob(logger, dataLayer, jobMonitorUtil, validator)
{
}