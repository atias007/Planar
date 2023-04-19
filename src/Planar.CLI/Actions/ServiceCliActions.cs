using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.DataProtect;
using Planar.CLI.Entities;
using Planar.CLI.Proxy;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
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
            var notnullRequest = FillLoginRequest(request);
            var response = await InnerLogin(notnullRequest, cancellationToken);
            if (response.Response.IsSuccessful)
            {
                ConnectUtil.SaveLoginRequest(request, LoginProxy.Token);
            }

            return response;
        }

        [Action("logout")]
        public static async Task<CliActionResponse> Logout(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            LoginProxy.Logout();
            ConnectUtil.Logout();
            RestProxy.Flush();
            return await Task.FromResult(CliActionResponse.Empty);
        }

        [Action("flush-logins")]
        public static async Task<CliActionResponse> FlushLogins(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ConnectUtil.Flush();
            return await Task.FromResult(CliActionResponse.Empty);
        }

        [Action("login-color")]
        public static async Task<CliActionResponse> LoginColor(CliLoginColorRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ConnectUtil.Current.Color = request.Color;
            // TODO: persist the changed color
            // TODO: add menu to set color
            return await Task.FromResult(CliActionResponse.Empty);
        }

        public static async Task InitializeLogin()
        {
            var request = ConnectUtil.GetSavedLoginRequestWithCredentials();
            if (request == null || !request.Remember) { return; }

            await InnerLogin(request);
        }

        private static CliLoginRequest FillLoginRequest(CliLoginRequest? request)
        {
            const string regexTepmplate = "^((6553[0-5])|(655[0-2][0-9])|(65[0-4][0-9]{2})|(6[0-4][0-9]{3})|([1-5][0-9]{4})|([0-5]{0,5})|([0-9]{1,4}))$";

            request ??= new CliLoginRequest();
            if (!InteractiveMode) { return request; }

            if (string.IsNullOrEmpty(request.Host))
            {
                request.Host = CollectCliValue("host", true, 1, 50, defaultValue: ConnectUtil.DefaultHost) ?? string.Empty;
            }

            if (request.Port == 0)
            {
                request.Port = int.Parse(CollectCliValue("port", true, 1, 5, regexTepmplate, "invalid port", ConnectUtil.DefaultPort.ToString()) ?? ConnectUtil.DefaultPort.ToString());
            }

            if (string.IsNullOrEmpty(request.Username))
            {
                request.Username = CollectCliValue("username", required: false, 2, 50);
            }

            if (string.IsNullOrEmpty(request.Password))
            {
                request.Password = CollectCliValue("password", required: false, 2, 50, secret: true);
            }

            var savedItem = ConnectUtil.GetSavedLogin(request.Key);
            if (savedItem != null)
            {
                request.Color = savedItem.Color;
                request.SecureProtocol = savedItem.SecureProtocol;
            }

            return request;
        }

        private static async Task<CliActionResponse> InnerLogin(CliLoginRequest request, CancellationToken cancellationToken = default)
        {
            var result = await LoginProxy.Login(request, cancellationToken);

            // Success authorize
            if (result.IsSuccessStatusCode)
            {
                return new CliActionResponse(result, message: $"login success ({LoginProxy.Role?.ToLower()})");
            }
            else if (result.StatusCode == HttpStatusCode.Conflict)
            {
                // No need to authorize
                RestProxy.Host = request.Host;
                RestProxy.Port = request.Port;
                RestProxy.SecureProtocol = request.SecureProtocol;
                RestProxy.Flush();

                LoginProxy.Logout();
                return CliActionResponse.Empty;
            }

            // Login error
            return new CliActionResponse(result);
        }
    }
}