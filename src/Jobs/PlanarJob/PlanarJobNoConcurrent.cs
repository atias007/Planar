﻿using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;

namespace Planar
{
    [DisallowConcurrentExecution]
    [PersistJobDataAfterExecution]
    public class PlanarJobNoConcurrent : PlanarJob
    {
        public PlanarJobNoConcurrent(
            ILogger<PlanarJob> logger,
            IJobPropertyDataLayer dataLayer,
            JobMonitorUtil jobMonitorUtil) : base(logger, dataLayer, jobMonitorUtil)
        {
        }
    }
}