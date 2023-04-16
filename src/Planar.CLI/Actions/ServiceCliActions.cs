using Microsoft.Extensions.Hosting;
using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.DataProtect;
using Planar.CLI.Entities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
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
            var response = await InnerLogin(request, cancellationToken);

            //// ConnectData.SetLoginRequest(request);
            return response;
        }

        [Action("logout")]
        public static async Task<CliActionResponse> Logout(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ConnectData.Logout();
            RestProxy.Username = null;
            RestProxy.Password = null;
            RestProxy.Token = null;
            RestProxy.Role = null;
            RestProxy.Flush();
            return await Task.FromResult(CliActionResponse.Empty);
        }

        public static async Task InitializeLogin()
        {
            var request = ConnectData.GetLoginRequest();
            await InnerLogin(request);
        }

        private static async Task<CliActionResponse> InnerLogin(CliLoginRequest? request, CancellationToken cancellationToken = default)
        {
            CliGeneral.Login.Set(request);
            if (request == null) { return CliActionResponse.Empty; }

            if (string.IsNullOrEmpty(request.Host))
            {
                request.Host = CollectCliValue("host", true, 1, 50, defaultValue: "localhost") ?? string.Empty;
            }

            if (request.Port == 0)
            {
                const string regexTepmplate = "^((6553[0-5])|(655[0-2][0-9])|(65[0-4][0-9]{2})|(6[0-4][0-9]{3})|([1-5][0-9]{4})|([0-5]{0,5})|([0-9]{1,4}))$";
                request.Port = int.Parse(CollectCliValue("port", true, 1, 5, regexTepmplate, "invalid port", "2306") ?? "0");
            }

            if (string.IsNullOrEmpty(request.User))
            {
                request.User = CollectCliValue("username", true, 2, 50);
            }

            if (string.IsNullOrEmpty(request.Password))
            {
                request.Password = CollectCliValue("password", true, 2, 50);
            }

            var schema = request.SSL ? "https" : "http";
            var body = new { Username = request.User, request.Password };
            var restRequest = new RestRequest("service/login", Method.Post);
            restRequest.AddBody(body);
            var options = new RestClientOptions
            {
                BaseUrl = new UriBuilder(schema, request.Host, request.Port).Uri,
                MaxTimeout = 10000,
            };

            var client = new RestClient(options);
            var result = await client.ExecuteAsync<LoginResponse>(restRequest, cancellationToken);
            if (result.IsSuccessStatusCode)
            {
                RestProxy.Host = request.Host;
                RestProxy.Port = request.Port;

                if (request.SSL)
                {
                    RestProxy.Schema = "https";
                }

                RestProxy.Username = request.User;
                RestProxy.Password = request.Password;
                RestProxy.Token = result.Data?.Token;
                RestProxy.Role = result.Data?.Role;
                RestProxy.Flush();
                return new CliActionResponse(result, message: $"login success ({result.Data?.Role.ToLower()})");
            }
            else if (result.StatusCode == HttpStatusCode.Conflict)
            {
                RestProxy.Host = request.Host;
                RestProxy.Port = request.Port;

                if (request.SSL)
                {
                    RestProxy.Schema = "https";
                }

                RestProxy.Username = null;
                RestProxy.Password = null;
                RestProxy.Token = null;
                RestProxy.Role = null;
                RestProxy.Flush();
                return CliActionResponse.Empty;
            }

            RestProxy.Flush();
            return new CliActionResponse(result);
        }
    }
}