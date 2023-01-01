using Microsoft.Extensions.DependencyInjection;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Planar.Service.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public class ClusterDomain : BaseBL<ClusterDomain, ClusterData>
    {
        public ClusterDomain(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<List<ClusterNode>> GetNodes()
        {
            return await DataLayer.GetClusterNodes();
        }

        public async Task<string> HealthCheck()
        {
            var serviceUnavaliable = false;
            var result = new StringBuilder();

            if (AppSettings.Clustering)
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
}