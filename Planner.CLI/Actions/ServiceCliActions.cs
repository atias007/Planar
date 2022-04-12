﻿using Planner.API.Common.Entities;
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
        public static async Task<CliActionResponse> StopScheduler(CliStopScheduler request)
        {
            var prm = new StopSchedulerRequest
            {
                WaitJobsToComplete = !request.Force
            };

            var restRequest = new RestRequest("service/stop", Method.Post)
                .AddBody(prm);

            await RestProxy.Invoke(restRequest);

            return CliActionResponse.Empty;
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
            return new CliActionResponse(result, result.Data?.Environment);
        }

        [Action("calendars")]
        public static async Task<CliActionResponse> GetAllCalendars()
        {
            var restRequest = new RestRequest("service/calendars", Method.Get);
            var result = await RestProxy.Invoke<List<string>>(restRequest);
            return new CliActionResponse(result, serializeObj: result.Data);
        }
    }
}