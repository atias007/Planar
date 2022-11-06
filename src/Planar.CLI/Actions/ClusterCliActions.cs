﻿using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using RestSharp;
using System;
using System.Collections.Generic;
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
        [Action("health-check")]
        public static async Task<CliActionResponse> HealthCheck()
        {
            var restRequest = new RestRequest("cluster/healthCheck", Method.Get);
            var result = await RestProxy.Invoke<string>(restRequest);
            return new CliActionResponse(result, result.Data);
        }
    }
}