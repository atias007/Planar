using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.General;
using Quartz;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PlanarRemoteJob;

public abstract class PlanarRemoteJob(
    ILogger logger,
    IJobPropertyDataLayer dataLayer,
    JobMonitorUtil jobMonitorUtil,
    IClusterUtil clusterUtil,
    IServiceProvider serviceProvider) : BaseProcessJob<PlanarJobRemoteProperties>(logger, dataLayer, jobMonitorUtil, clusterUtil)
{
    public override async Task Execute(IJobExecutionContext context)
    {
        try
        {
            MqttBrokerService.RegisterInterceptingPublish(InterceptingPublishAsync, context.FireInstanceId);
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
        }
    }
}