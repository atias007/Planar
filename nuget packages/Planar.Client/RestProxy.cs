using Planar.Client.Exceptions;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client
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
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        private string Token { get; set; }
        private HttpClient _client;

#else
        public string? Username { get; private set; }
        public string? Password { get; private set; }
        public string? Role { get; private set; }
        public string? LastName { get; private set; }
        public string FirstName { get; private set; } = null!;
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
            var assembly = typeof(PlanarClient).Assembly;
            var version = assembly.GetName().Version;
            if (version != null)
            {
                var versionString = $"{version.Major}.{version.Minor}.{version.Build}";
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Planar.Client", versionString));
            }
            else
            {
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Planar.Client"));
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

        private static async Task ValidateResponse(RestResponse response)
        {
            if (response.IsSuccess) { return; }

            await HandleBadResponse(response);

            if (response.StatusCode == HttpStatusCode.Conflict) { throw new PlanarConflictException(response); }
            if (response.StatusCode == HttpStatusCode.Forbidden) { throw new PlanarForbiddenException(response); }
            if (response.StatusCode == HttpStatusCode.RequestTimeout) { throw new PlanarRequestTimeoutException(response); }
            if (response.StatusCode == HttpStatusCode.ServiceUnavailable) { throw new PlanarServiceUnavailableException(response); }
            if (response.StatusCode == HttpStatusCode.Unauthorized) { throw new PlanarUnauthorizedException(response); }

#if NETSTANDARD2_0
            if ((int)response.StatusCode == 429) { throw new PlanarTooManyRequestsException(response); }

#else
            if (response.StatusCode == HttpStatusCode.TooManyRequests) { throw new PlanarTooManyRequestsException(response); }
#endif

            await HandleNotFoundResponse(response);

            throw new PlanarException(response);
        }

        private static async Task HandleNotFoundResponse(RestResponse response)
        {
            if (response.StatusCode != HttpStatusCode.NotFound) { return; }
            if (string.IsNullOrWhiteSpace(await response.GetStringContent()))
            {
                throw new PlanarNotFoundException(response);
            }

            throw new PlanarNotFoundException(await response.GetStringContent() ?? string.Empty);
        }

        private async static Task HandleBadResponse(RestResponse response)
        {
            if (response.StatusCode != HttpStatusCode.BadRequest) { return; }
            if (!string.IsNullOrWhiteSpace(await response.GetStringContent()))
            {
#if NETSTANDARD2_0
                PlanarValidationErrors errorResponse = null;

#else
                PlanarValidationErrors? errorResponse = null;

#endif

                try
                {
                    errorResponse = CoreSerializer.Deserialize<PlanarValidationErrors>(await response.GetStringContent());
                }
                catch
                {
                    // *** DO NOTHING ***
                }

                if (errorResponse?.Errors.Any() ?? false)
                {
                    throw new PlanarValidationException("Planar service return multiple validation errors. For more detais see errors property", errorResponse);
                }

                if (!string.IsNullOrWhiteSpace(errorResponse?.Detail))
                {
                    throw new PlanarValidationException(errorResponse.Detail);
                }
            }

            await HandleODataErrorResponse(response);
            throw new PlanarValidationException("Planar service return validation errors");
        }

        private async static Task HandleODataErrorResponse(RestResponse response)
        {
            if (response.StatusCode != HttpStatusCode.BadRequest) { return; }
            var content = await response.GetStringContent();
            if (string.IsNullOrWhiteSpace(content)) { return; }

            using (var doc = JsonDocument.Parse(content))
            {
                var root = doc.RootElement;

                if (root.TryGetProperty("error", out var errorElement) &&
                    errorElement.TryGetProperty("innererror", out var innerElement) &&
                    innerElement.TryGetProperty("message", out var messageElement))
                {
                    var message = messageElement.GetString() ?? string.Empty;
                    message = ClearMessage(message);
                    throw new PlanarValidationException(message);
                }

                if (root.TryGetProperty("error", out var errorElement2) &&
                    errorElement2.TryGetProperty("message", out var messageElement2))
                {
                    var message = messageElement2.GetString() ?? string.Empty;
                    message = ClearMessage(message);
                    throw new PlanarValidationException(message);
                }
            }
        }

        private static string ClearMessage(string message)
        {
            var index = message.IndexOf("on type '");
            if (index < 0) { return message; }

#if NETSTANDARD2_0
            return message.Substring(0, index).ToLower();
#else
            return message[0..index].ToLower();
#endif
        }

        public async Task<TResponse> InvokeAsync<TResponse>(RestRequest request, CancellationToken cancellationToken) where TResponse : class
        {
            var response = await ExecuteAsync<TResponse>(request, cancellationToken);
            if (await RefreshToken(response, cancellationToken))
            {
                response = await ExecuteAsync<TResponse>(request, cancellationToken);
            }

            await ValidateResponse(response);
#if NETSTANDARD2_0
            return response.Data;
#else
            return response.Data!;
#endif
        }

#if NETSTANDARD2_0

        public async Task<string> InvokeAsync(RestRequest request, CancellationToken cancellationToken)
#else
        public async Task<string?> InvokeAsync(RestRequest request, CancellationToken cancellationToken)
#endif

        {
            var response = await ExecuteAsync(request, cancellationToken);
            if (await RefreshToken(response, cancellationToken))
            {
                response = await ExecuteAsync(request, cancellationToken);
            }

            await ValidateResponse(response);
            return await response.GetStringContent();
        }

        public async Task<T> InvokeScalarAsync<T>(RestRequest request, CancellationToken cancellationToken) where T : struct
        {
            var response = await ExecuteAsync(request, cancellationToken);
            if (await RefreshToken(response, cancellationToken))
            {
                response = await ExecuteAsync(request, cancellationToken);
            }

            await ValidateResponse(response);
            var text = await response.GetStringContent();
            if (string.IsNullOrWhiteSpace(text)) { return default; }
            var obj = Convert.ChangeType(text, typeof(T));
            return (T)obj;
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
            var request = restRequest.GetRequest();
            HttpResponseMessage response;

            if (restRequest.Timeout != null && restRequest.Timeout != TimeSpan.Zero)
            {
#if NETSTANDARD2_0

                using (var cts = new CancellationTokenSource(restRequest.Timeout))
#else
                using (var cts = new CancellationTokenSource(restRequest.Timeout.Value))
#endif
                {
                    using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token))
                    {
                        response = await Client.SendAsync(request, linkedCts.Token);
                    }
                }
            }
            else
            {
                response = await Client.SendAsync(request, cancellationToken);
            }

            var result = new RestResponse(response);
            return await result.GetTypedResponse<TResult>();
        }

        private async Task<RestResponse> ExecuteAsync(RestRequest restRequest, CancellationToken cancellationToken)
        {
            var request = restRequest.GetRequest();
            var response = await Client.SendAsync(request, cancellationToken);
            var result = new RestResponse(response);
            return result;
        }

        public async Task<RestResponse<LoginResponse>> Login(LoginData login, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(login.Host)) { throw new PlanarException("Host is mandatory"); }
            if (login.Timeout.HasValue)
            {
                if (login.Timeout == TimeSpan.Zero) { throw new PlanarException("Login timeout must be greater the 0"); }
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

                if (result.IsSuccess)
                {
                    var data = await result.GetData<LoginResponse>()
                        ?? throw new PlanarException("The data return from login service is invalid");

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