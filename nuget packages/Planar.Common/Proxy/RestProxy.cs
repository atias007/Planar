using Planar.Job;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Common.Proxy
{
    internal class RestProxy
    {
        public const string DefaultHost = "localhost";
        public const int DefaultPort = 2306;
        public const int DefaultSecurePort = 2610;

        private readonly object _lock = new object();

        public string Host { get; private set; } = DefaultHost;
        public int Port { get; private set; } = DefaultPort;
        public bool SecureProtocol { get; private set; }

#if NETSTANDARD2_0

        public string Username { get; private set; }
        public string Password { get; private set; }
        public string Role { get; private set; }
        private string Token { get; set; }
        private HttpClient _client;

#else
        public string? Username { get; private set; }
        public string? Password { get; private set; }
        public string? Role { get; private set; }
        private string? Token { get; set; }
        private HttpClient? _client;

#endif

        public TimeSpan Timeout { get; private set; } = TimeSpan.FromSeconds(10);

        private Uri BaseUri => new UriBuilder(Schema, Host, Port).Uri;

#if NETSTANDARD2_0

        internal static HttpClient CreateHttpClient(Uri baseAddress, string token = null, TimeSpan? timeout = null)
#else
        internal static HttpClient CreateHttpClient(Uri baseAddress, string? token = null, TimeSpan? timeout = null)
#endif

        {
            if (timeout == null || timeout == TimeSpan.Zero)
            {
                timeout = TimeSpan.FromSeconds(10);
            }

            var client = new HttpClient
            {
                Timeout = timeout.GetValueOrDefault(),
                BaseAddress = baseAddress,
            };

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // get the assembly version number
            var assembly = typeof(RestProxy).Assembly;
            var version = assembly.GetName().Version;
            if (version != null)
            {
                var versionString = $"{version.Major}.{version.Minor}.{version.Build}";
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Planar.Common.Proxy", versionString));
            }
            else
            {
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Planar.Common.Proxy"));
            }

            return client;
        }

        private HttpClient Client
        {
            get
            {
                if (_client != null) { return _client; }

                lock (_lock)
                {
                    if (_client != null) { return _client; }
                    _client = CreateHttpClient(BaseUri, Token, Timeout);
                }

                return _client;
            }
        }

        private string Schema => GetSchema(SecureProtocol);

        private static string GetSchema(bool secureProtocol)
        {
            return secureProtocol ? "https" : "http";
        }

        private async Task<bool> RefreshToken(RestResponse response, CancellationToken cancellationToken)
        {
            if (!IsAuthorized) { return false; }
            if (response.StatusCode != HttpStatusCode.Unauthorized) { return false; }

            var reloginResponse = await Relogin(cancellationToken);
            return reloginResponse.IsSuccess;
        }

        public async Task<RestResponse<TResponse>> InvokeAsync<TResponse>(RestRequest request, CancellationToken cancellationToken) where TResponse : class
        {
            var response = await ExecuteAsync<TResponse>(request, cancellationToken);
            if (await RefreshToken(response, cancellationToken))
            {
                response = await ExecuteAsync<TResponse>(request, cancellationToken);
            }

#if NETSTANDARD2_0
            return response;
#else
            return response;
#endif
        }

        public async Task<RestResponse> InvokeAsync(RestRequest request, CancellationToken cancellationToken)
        {
            var response = await ExecuteAsync(request, cancellationToken);
            if (await RefreshToken(response, cancellationToken))
            {
                response = await ExecuteAsync(request, cancellationToken);
            }

            return response;
        }

        private async Task<RestResponse> Relogin(CancellationToken cancellationToken)
        {
            var login = new LoginData
            {
                Host = Host,
                Port = Port,
                SecureProtocol = SecureProtocol,
                Username = Username,
                Password = Password,
            };

            return await Login(login, cancellationToken);
        }

        private void Flush()
        {
            lock (_lock)
            {
                _client = null;
            }
        }

        private async Task<RestResponse<TResult>> ExecuteAsync<TResult>(RestRequest restRequest, CancellationToken cancellationToken)
            where TResult : class
        {
            var response = await SendAsync(restRequest, cancellationToken);
            var result = new RestResponse(response);
            return await result.GetTypedResponse<TResult>();
        }

        private async Task<RestResponse> ExecuteAsync(RestRequest restRequest, CancellationToken cancellationToken)
        {
            var response = await SendAsync(restRequest, cancellationToken);
            var result = new RestResponse(response);
            return result;
        }

        private async Task<HttpResponseMessage> SendAsync(RestRequest restRequest, CancellationToken cancellationToken)
        {
            HttpResponseMessage response;
            var request = restRequest.GetRequest();

            if (restRequest.Timeout != null && restRequest.Timeout != TimeSpan.Zero)
            {
#if NETSTANDARD2_0

                using (var cts = new CancellationTokenSource(restRequest.Timeout))
                {
                    using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token))
                    {
                        response = await Client.SendAsync(request, linkedCts.Token);
                    }
                }
#else
                using var cts = new CancellationTokenSource(restRequest.Timeout.Value);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
                response = await Client.SendAsync(request, linkedCts.Token);
#endif
            }
            else
            {
                response = await Client.SendAsync(request, cancellationToken);
            }

            return response;
        }

        public async Task<RestResponse<LoginResponse>> Login(LoginData login, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(login.Host)) { throw new PlanarJobException("Host is mandatory"); }
            if (login.Timeout.HasValue)
            {
                if (login.Timeout == TimeSpan.Zero) { throw new PlanarJobException("Login timeout must be greater the 0"); }
                Timeout = login.Timeout.Value;
            }

            var schema = GetSchema(login.SecureProtocol);
            var baseAddress = new UriBuilder(schema, login.Host, login.Port).Uri;

            using (var client = CreateHttpClient(baseAddress, null, Timeout))
            {
                var restRequest = new RestRequest("service/login", HttpMethod.Post);
                var body = new { login.Username, login.Password };
                restRequest.AddBody(body);

                var request = restRequest.GetRequest();
                var response = await Client.SendAsync(request, cancellationToken);
                var result = new RestResponse(response);
                if (result.StatusCode == HttpStatusCode.Conflict)
                {
                    Host = login.Host;
                    Port = login.Port;
                    SecureProtocol = login.SecureProtocol;
                    Flush();

                    Username = login.Username;
                    Password = login.Password;
                }
                else if (result.IsSuccess)
                {
                    var data = await result.GetData<LoginResponse>()
                        ?? throw new PlanarJobException("The data return from login service is invalid");

                    Host = login.Host;
                    Port = login.Port;
                    SecureProtocol = login.SecureProtocol;
                    Flush();

                    Username = login.Username;
                    Password = login.Password;
                    Token = data.Token;
                    Role = data.Role;
                }

                return await result.GetTypedResponse<LoginResponse>();
            }
        }

        public void Logout()
        {
            Username = null;
            Password = null;
            Token = null;
            Role = null;
        }

        public bool IsAuthorized => !(string.IsNullOrEmpty(Token) || string.IsNullOrEmpty(Password) || string.IsNullOrEmpty(Username));
    }
}