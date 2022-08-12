using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("cluster")]
    public class ClusterCliActions : BaseCliAction<ClusterCliActions>
    {
        [Action("nodes")]
        public static async Task<CliActionResponse> GetClusterNodes()
        {
            var restRequest = new RestRequest("cluster/nodes", Method.Get);
            return await ExecuteTable<List<CliClusterNode>>(restRequest, CliTableExtensions.GetTable);
        }

        [Action("hc")]
        [Action("healthcheck")]
        public static async Task<CliActionResponse> HealthCheck()
        {
            var restRequest = new RestRequest("cluster/nodes", Method.Get);
            var result = await RestProxy.Invoke<List<CliClusterNode>>(restRequest);
            string message = null;
            if (result.IsSuccessful)
            {
                message = result.Data.All(d => d.LiveNode).ToString();
            }

            return new CliActionResponse(result, message: message);
        }
    }
}