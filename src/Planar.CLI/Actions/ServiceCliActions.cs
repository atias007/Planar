﻿using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
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
        public static async Task<CliActionResponse> GetInfo(CliGetInfoRequest request)
        {
            if (string.IsNullOrEmpty(request.Key))
            {
                var restRequest = new RestRequest("service", Method.Get);
                var result = await RestProxy.Invoke<GetServiceInfoResponse>(restRequest);

                if (result.IsSuccessful && result.Data != null)
                {
                    result.Data.CliVersion = Program.Version;
                }

                return new CliActionResponse(result, result.Data);
            }
            else
            {
                var key = request.Key.Replace(" ", string.Empty).ToLower();
                if (key == "cliversion")
                {
                    return new CliActionResponse(new RestResponse { StatusCode = HttpStatusCode.OK, ResponseStatus = ResponseStatus.Completed, IsSuccessStatusCode = true });
                }

                var restRequest = new RestRequest("service/{key}", Method.Get);
                restRequest.AddUrlSegment("key", request.Key);
                var result = await RestProxy.Invoke<string>(restRequest);
                return new CliActionResponse(result, result.Data);
            }
        }

        [Action("version")]
        public static async Task<CliActionResponse> GetVersion()
        {
            var restRequest = new RestRequest("service", Method.Get);
            var result = await RestProxy.Invoke<GetServiceInfoResponse>(restRequest);

            if (result.IsSuccessful && result.Data != null)
            {
                result.Data.CliVersion = Program.Version;

                var versionData = new { result.Data.ServiceVersion, result.Data.CliVersion };
                return new CliActionResponse(result, versionData);
            }

            return new CliActionResponse(result);
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

        [Action("loglevel")]
        public static async Task<CliActionResponse> GetLogLevel()
        {
            var restRequest = new RestRequest("service", Method.Get);
            var result = await RestProxy.Invoke<GetServiceInfoResponse>(restRequest);
            return new CliActionResponse(result, message: result.Data?.LogLevel);
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