using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;

namespace Planar;

public class PlanarJobConcurrent(
    ILogger<PlanarJobConcurrent> logger,
    IJobPropertyDataLayer dataLayer,
    JobMonitorUtil jobMonitorUtil) : PlanarJob(logger, dataLayer, jobMonitorUtil)
{
}