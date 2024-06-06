using Planar.API.Common.Entities;
using Planar.CLI.Entities;
using RestSharp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.Proxy
{
    internal static class LoginProxy
    {
        public static string? Username { get; set; }
        public static string? Password { get; set; }
        public static string? Token { get; set; }
        public static string? Role { get; set; }

        public static async Task<RestResponse> Relogin(CancellationToken cancellationToken)
        {
            var login = new LoginData
            {
                Host = RestProxy.Host,
                Port = RestProxy.Port,
                SecureProtocol = RestProxy.SecureProtocol,
                Username = Username,
                Password = Password,
            };

            return await Login(login, cancellationToken);
        }

        public static async Task<RestResponse<LoginResponse>> Login(CliLoginRequest request, CancellationToken cancellationToken)
        {
            var login = new LoginData
            {
                Host = request.Host,
                Port = request.Port,
                SecureProtocol = request.SecureProtocol,
                Username = request.Username,
                Password = request.Password,
            };

            return await Login(login, cancellationToken);
        }

        public static async Task<RestResponse<LoginResponse>> Login(LoginData login, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(login.Host)) { throw new CliException("host is mandatory"); }
            var schema = RestProxy.GetSchema(login.SecureProtocol);

            var options = new RestClientOptions
            {
                BaseUrl = new UriBuilder(schema, login.Host, login.Port).Uri,
                Timeout = TimeSpan.FromMilliseconds(10_000),
            };

            var client = new RestClient(options);
            var restRequest = new RestRequest("service/login", Method.Post);
            var body = new { login.Username, login.Password };
            restRequest.AddBody(body);

            var result = await client.ExecuteAsync<LoginResponse>(restRequest, cancellationToken);
            if (result.IsSuccessStatusCode)
            {
                if (result.Data == null) { throw new CliException("the data from login service is null"); }

                RestProxy.Host = login.Host;
                RestProxy.Port = login.Port;
                RestProxy.SecureProtocol = login.SecureProtocol;
                RestProxy.Flush();

                Username = login.Username;
                Password = login.Password;
                Token = result.Data.Token;
                Role = result.Data.Role;
            }

            return result;
        }

        public static void Logout()
        {
            Username = null;
            Password = null;
            Token = null;
            Role = null;
        }

        public static bool IsAuthorized => !(string.IsNullOrEmpty(Token) || string.IsNullOrEmpty(Password) || string.IsNullOrEmpty(Username));
    }
}