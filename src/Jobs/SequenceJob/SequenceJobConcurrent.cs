using CommonJob;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.General;
using Quartz;

namespace Planar;

[PersistJobDataAfterExecution]
public class SequenceJobConcurrent(
    ILogger<SequenceJobConcurrent> logger,
    IJobPropertyDataLayer dataLayer,
    JobMonitorUtil jobMonitorUtil,
    IClusterUtil clusterUtil,
    IValidator<SequenceJobProperties> validator) : SequenceJob(logger, dataLayer, jobMonitorUtil, clusterUtil, validator)
{
}