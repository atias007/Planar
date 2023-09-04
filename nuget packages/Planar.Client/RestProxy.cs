using RestSharp;
using System.Net;

namespace Planar.Client
{
    internal struct LoginData
    {
        public string? Host { get; set; }
        public bool SecureProtocol { get; set; }
        public int Port { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    internal class RestProxy
    {
        public const string DefaultHost = "localhost";
        public const int DefaultPort = 2306;
        public const int DefaultSecurePort = 2610;

        private readonly object _lock = new object();
        private RestClient? _client;

        public string Host { get; set; } = DefaultHost;
        public int Port { get; set; } = DefaultPort;
        public bool SecureProtocol { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
        public string? Role { get; private set; }
        public string FirstName { get; set; } = null!;
        public string? LastName { get; set; }
        private string? Token { get; set; }

        private Uri BaseUri => new UriBuilder(Schema, Host, Port).Uri;

        private RestClient Proxy
        {
            get
            {
                lock (_lock)
                {
                    if (_client == null)
                    {
                        var options = new RestClientOptions
                        {
                            BaseUrl = BaseUri,
                            MaxTimeout = Convert.ToInt32(Timeout.TotalMilliseconds)
                        };

                        _client = new RestClient(options);

                        if (!string.IsNullOrEmpty(Token))
                        {
                            _client.AddDefaultHeader("Authorization", $"Bearer {Token}");
                        }
                    }

                    return _client;
                }
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
            return reloginResponse.IsSuccessful;
        }

        public async Task<RestResponse<TResponse>> Invoke<TResponse>(RestRequest request, CancellationToken cancellationToken)
        {
            var response = await Proxy.ExecuteAsync<TResponse>(request, cancellationToken);
            if (await RefreshToken(response, cancellationToken))
            {
                response = await Proxy.ExecuteAsync<TResponse>(request, cancellationToken);
            }

            return response;
        }

        public async Task<RestResponse> Invoke(RestRequest request, CancellationToken cancellationToken)
        {
            var response = await Proxy.ExecuteAsync(request, cancellationToken);
            if (await RefreshToken(response, cancellationToken))
            {
                response = await Proxy.ExecuteAsync(request, cancellationToken);
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

        public async Task<RestResponse<LoginResponse>> Login(LoginData login, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(login.Host)) { throw new PlanarException("host is mandatory"); }
            var schema = GetSchema(login.SecureProtocol);

            var options = new RestClientOptions
            {
                BaseUrl = new UriBuilder(schema, login.Host, login.Port).Uri,
                MaxTimeout = Convert.ToInt32(Timeout.TotalMilliseconds),
            };

            var client = new RestClient(options);
            var restRequest = new RestRequest("service/login", Method.Post);
            var body = new { login.Username, login.Password };
            restRequest.AddBody(body);

            var result = await client.ExecuteAsync<LoginResponse>(restRequest, cancellationToken);
            if (result.IsSuccessStatusCode)
            {
                if (result.Data == null) { throw new PlanarException("the data from login service is null"); }

                Host = login.Host;
                Port = login.Port;
                SecureProtocol = login.SecureProtocol;
                Flush();

                Username = login.Username;
                Password = login.Password;
                Token = result.Data.Token;
                Role = result.Data.Role;
            }

            return result;
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