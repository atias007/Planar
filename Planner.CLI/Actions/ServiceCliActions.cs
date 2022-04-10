using Planner.API.Common.Entities;
using Planner.CLI.Attributes;
using Planner.CLI.Entities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planner.CLI.Actions
{
    [Module("service")]
    public class ServiceCliActions : BaseCliAction<ServiceCliActions>
    {
        [Action("stop")]
        public static async Task<ActionResponse> StopScheduler(CliStopScheduler request)
        {
            var prm = new StopSchedulerRequest
            {
                WaitJobsToComplete = !request.Force
            };

            var restRequest = new RestRequest("service/stop", Method.Post)
                .AddBody(prm);

            await RestProxy.Invoke(restRequest);

            return ActionResponse.Empty;
        }

        [Action("isalive")]
        public static async Task<ActionResponse> IsAlive()
        {
            var restRequest = new RestRequest("service", Method.Get);
            var result = await RestProxy.Invoke<GetServiceInfoResponse>(restRequest);
            var message = (result.IsStarted && result.IsShutdown == false && result.InStandbyMode == false).ToString().ToLower();
            return new ActionResponse(result, message);
        }

        [Action("env")]
        public static async Task<ActionResponse> GetEnvironment()
        {
            var restRequest = new RestRequest("service", Method.Get);
            var result = await RestProxy.Invoke<GetServiceInfoResponse>(restRequest);
            return new ActionResponse(result, result.Environment);
        }

        [Action("calendars")]
        public static async Task<ActionResponse> GetAllCalendars()
        {
            var restRequest = new RestRequest("service/calendars", Method.Get);
            var result = await RestProxy.Invoke<List<string>>(restRequest);
            return new ActionResponse(BaseResponse.Empty, serializeObj: result);
        }
    }
}