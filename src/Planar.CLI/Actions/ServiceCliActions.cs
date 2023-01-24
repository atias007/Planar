using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.DataProtect;
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
                    var response = GetGenericSuccessRestResponse();
                    return new CliActionResponse(response);
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
        [Action("health-check")]
        public static async Task<CliActionResponse> HealthCheck()
        {
            var restRequest = new RestRequest("service/healthCheck", Method.Get);
            var result = await RestProxy.Invoke<string>(restRequest);
            return new CliActionResponse(result, result.Data);
        }

        [Action("env")]
        public static async Task<CliActionResponse> GetEnvironment()
        {
            var restRequest = new RestRequest("service", Method.Get);
            var result = await RestProxy.Invoke<GetServiceInfoResponse>(restRequest);
            return new CliActionResponse(result, message: result.Data?.Environment);
        }

        [Action("log-level")]
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

        [Action("login")]
        public static async Task<CliActionResponse> Login(CliLoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Host))
            {
                request.Host = "127.0.0.1";
            }

            if (request.Port == 0)
            {
                request.Port = 2306;
            }

            RestProxy.Host = request.Host;
            RestProxy.Port = request.Port;

            if (request.SSL)
            {
                RestProxy.Schema = "https";
            }

            RestProxy.Flush();
            ConnectData.SetLoginRequest(request);
            return await Task.FromResult(CliActionResponse.Empty);
        }

        public static void InitializeLogin()
        {
            var request = ConnectData.GetLoginRequest();
            if (request == null) { return; }
            Login(request).Wait();
        }
    }
}