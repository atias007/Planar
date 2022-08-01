using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("service")]
    public class ServiceCliActions : BaseCliAction<ServiceCliActions>
    {
        [Action("stop")]
        public static async Task<CliActionResponse> StopScheduler()
        {
            var restRequest = new RestRequest("service/stop", Method.Post);
            return await Execute(restRequest);
        }

        [Action("start")]
        public static async Task<CliActionResponse> StartScheduler()
        {
            var restRequest = new RestRequest("service/start", Method.Post);
            return await Execute(restRequest);
        }

        [Action("info")]
        public static async Task<CliActionResponse> GetInfo()
        {
            var restRequest = new RestRequest("service", Method.Get);
            var result = await RestProxy.Invoke<GetServiceInfoResponse>(restRequest);
            return new CliActionResponse(result, result.Data);
        }

        [Action("hc")]
        [Action("healthcheck")]
        public static async Task<CliActionResponse> HealthCheck()
        {
            var restRequest = new RestRequest("service/healthCheck", Method.Get);
            var result = await RestProxy.Invoke<bool>(restRequest);
            return new CliActionResponse(result, result.Data);
        }

        [Action("env")]
        public static async Task<CliActionResponse> GetEnvironment()
        {
            var restRequest = new RestRequest("service", Method.Get);
            var result = await RestProxy.Invoke<GetServiceInfoResponse>(restRequest);
            return new CliActionResponse(result, message: result.Data?.Environment);
        }

        [Action("calendars")]
        public static async Task<CliActionResponse> GetAllCalendars()
        {
            var restRequest = new RestRequest("service/calendars", Method.Get);
            return await ExecuteEntity<List<string>>(restRequest);
        }

        [Action("connect")]
        public static async Task<CliActionResponse> Connect(CliConnectRequest request)
        {
            RestProxy.Host = request.Host;
            RestProxy.Port = request.Port;

            if (request.SSL)
            {
                RestProxy.Schema = "https";
            }

            RestProxy.Flush();

            return await Task.FromResult(CliActionResponse.Empty);
        }
    }
}