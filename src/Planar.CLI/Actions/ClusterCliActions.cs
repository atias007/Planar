using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using Planar.CLI.Proxy;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("cluster", "Actions to monitor cluster nodes, test communication and do some operatins")]
    public class ClusterCliActions : BaseCliAction<ClusterCliActions>
    {
        [Action("nodes")]
        public static async Task<CliActionResponse> GetClusterNodes(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("cluster/nodes", Method.Get);
            return await ExecuteTable<List<CliClusterNode>>(restRequest, CliTableExtensions.GetTable, cancellationToken);
        }

        [Action("hc")]
        [Action("health-check")]
        public static async Task<CliActionResponse> HealthCheck(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("cluster/healthCheck", Method.Get);
            var result = await RestProxy.Invoke<string>(restRequest, cancellationToken);
            return new CliActionResponse(result, message: result.Data);
        }

        [Action("max-concurrency")]
        public static async Task<CliActionResponse> MaxConcurrency(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("cluster/maxConcurrency", Method.Get);
            var result = await RestProxy.Invoke<int>(restRequest, cancellationToken);
            return new CliActionResponse(result, message: result.Data.ToString());
        }
    }
}