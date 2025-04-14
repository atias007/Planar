using CommonJob;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.General;
using Quartz;

namespace Planar;

[DisallowConcurrentExecution]
[PersistJobDataAfterExecution]
internal class SequenceJobNoConcurrent(
    ILogger<SequenceJobNoConcurrent> logger,
    IJobPropertyDataLayer dataLayer,
    JobMonitorUtil jobMonitorUtil,
    IClusterUtil clusterUtil,
    IValidator<SequenceJobProperties> validator) : SequenceJob(logger, dataLayer, jobMonitorUtil, clusterUtil, validator)
{
}