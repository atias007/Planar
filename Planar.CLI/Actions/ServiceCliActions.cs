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
        public static async Task<CliActionResponse> StopScheduler(CliStopScheduler request)
        {
            var prm = new StopSchedulerRequest
            {
                WaitJobsToComplete = !request.Force
            };

            var restRequest = new RestRequest("service/stop", Method.Post)
                .AddBody(prm);

            return await Execute(restRequest);
        }

        [Action("isalive")]
        public static async Task<CliActionResponse> IsAlive()
        {
            var restRequest = new RestRequest("service", Method.Get);
            var result = await RestProxy.Invoke<GetServiceInfoResponse>(restRequest);
            var message =
                result.IsSuccessful ?
                (result.Data.IsStarted && result.Data.IsShutdown == false && result.Data.InStandbyMode == false).ToString().ToLower() :
                string.Empty;

            return new CliActionResponse(result, message);
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

            return await Task.FromResult(CliActionResponse.Empty);
        }
    }
}