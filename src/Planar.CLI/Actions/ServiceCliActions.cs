using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.DataProtect;
using Planar.CLI.Entities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("service", "Actions to operate service, check alive, list calendars and more")]
    public class ServiceCliActions : BaseCliAction<ServiceCliActions>
    {
        [Action("halt")]
        public static async Task<CliActionResponse> HaltScheduler(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("service/halt", Method.Post);
            return await Execute(restRequest, cancellationToken);
        }

        [Action("start")]
        public static async Task<CliActionResponse> StartScheduler(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("service/start", Method.Post);
            return await Execute(restRequest, cancellationToken);
        }

        [Action("info")]
        public static async Task<CliActionResponse> GetInfo(CliGetInfoRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(request.Key))
            {
                var restRequest = new RestRequest("service", Method.Get);
                var result = await RestProxy.Invoke<GetServiceInfoResponse>(restRequest, cancellationToken);

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
                    return CliActionResponse.Empty;
                }

                var restRequest = new RestRequest("service/{key}", Method.Get);
                restRequest.AddUrlSegment("key", request.Key);
                var result = await RestProxy.Invoke<string>(restRequest, cancellationToken);
                return new CliActionResponse(result, result.Data);
            }
        }

        [Action("version")]
        public static async Task<CliActionResponse> GetVersion(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("service", Method.Get);
            var result = await RestProxy.Invoke<GetServiceInfoResponse>(restRequest, cancellationToken);

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
        public static async Task<CliActionResponse> HealthCheck(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("service/healthCheck", Method.Get);
            var result = await RestProxy.Invoke<string>(restRequest, cancellationToken);
            return new CliActionResponse(result, result.Data);
        }

        [Action("env")]
        public static async Task<CliActionResponse> GetEnvironment(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("service", Method.Get);
            var result = await RestProxy.Invoke<GetServiceInfoResponse>(restRequest, cancellationToken);
            return new CliActionResponse(result, message: result.Data?.Environment);
        }

        [Action("log-level")]
        public static async Task<CliActionResponse> GetLogLevel(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("service", Method.Get);
            var result = await RestProxy.Invoke<GetServiceInfoResponse>(restRequest, cancellationToken);
            return new CliActionResponse(result, message: result.Data?.LogLevel);
        }

        [Action("calendars")]
        public static async Task<CliActionResponse> GetAllCalendars(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("service/calendars", Method.Get);
            return await ExecuteEntity<List<string>>(restRequest, cancellationToken);
        }

        [Action("login")]
        public static async Task<CliActionResponse> Login(CliLoginRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            InnerLogin(request);

            ConnectData.SetLoginRequest(request);
            return await Task.FromResult(CliActionResponse.Empty);
        }

        [Action("logout")]
        public static async Task<CliActionResponse> Logout(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ConnectData.Logout();
            InnerLogin(new CliLoginRequest());
            return await Task.FromResult(CliActionResponse.Empty);
        }

        public static void InitializeLogin()
        {
            var request = ConnectData.GetLoginRequest();
            InnerLogin(request);
        }

        private static void InnerLogin(CliLoginRequest? request)
        {
            CliGeneral.Login.Set(request);
            if (request == null) { return; }

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
        }
    }
}