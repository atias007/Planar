using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Planar.Common;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Planar.Service.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planar.Service.API;

public class ClusterDomain(IServiceProvider serviceProvider) : BaseLazyBL<ClusterDomain, ClusterData>(serviceProvider)
{
    public async Task<List<ClusterNode>> GetNodes()
    {
        var result = await DataLayer.GetClusterNodes();
        if (!AppSettings.Cluster.Clustering && result.Count == 1)
        {
            var hc = SchedulerUtil.IsSchedulerRunning;
            if (hc)
            {
                result[0].HealthCheckDate = DateTime.Now;
            }
            else
            {
                result[0].HealthCheckDate = DateTime.Now.Add(-AppSettings.Cluster.CheckinInterval).AddMinutes(-1);
            }
        }

        return result;
    }

    public async Task<int> MaxConcurrency()
    {
        var nodes = await GetNodes();
        var result = nodes.Where(n => n.LiveNode).Sum(n => n.MaxConcurrency);
        return result;
    }

    public async Task<string> HealthCheck()
    {
        var serviceUnavaliable = false;
        var result = new StringBuilder();

        if (AppSettings.Cluster.Clustering)
        {
            var util = _serviceProvider.GetRequiredService<ClusterUtil>();
            var hc = await util.HealthCheck();

            if (hc)
            {
                result.AppendLine("Cluster healthy");
            }
            else
            {
                serviceUnavaliable = true;
                result.AppendLine("Cluster unhealthy");
            }
        }
        else
        {
            result.AppendLine("Cluster: [Clustering not enabled, skip health check]");
        }

        var message = result.ToString().Trim();
        if (serviceUnavaliable)
        {
            throw new RestServiceUnavailableException(message);
        }

        return message;
    }
}