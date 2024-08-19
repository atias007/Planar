using Core.JsonConvertor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Planar.Client.Entities;
using Planar.Client.Exceptions;
using Planar.Client.Serialize;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;
using System;
using System.Linq;
using System.Net;
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
        private RestClient? _client;

        public string Host { get; private set; } = DefaultHost;
        public int Port { get; private set; } = DefaultPort;
        public bool SecureProtocol { get; private set; }
        public string? Username { get; private set; }
        public string? Password { get; private set; }
        public TimeSpan Timeout { get; private set; } = TimeSpan.FromSeconds(10);
        public string? Role { get; private set; }
        public string FirstName { get; private set; } = null!;
        public string? LastName { get; private set; }
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
                            Timeout = TimeSpan.FromMilliseconds(Convert.ToInt32(Timeout.TotalMilliseconds))
                        };

                        var serOprions = new JsonSerializerSettings();
                        serOprions.Converters.Add(new NewtonsoftTimeSpanConverter());
                        serOprions.Converters.Add(new NewtonsoftNullableTimeSpanConverter());
                        serOprions.Converters.Add(new GenericEnumConverter<JobActiveMembers>());
                        serOprions.Converters.Add(new GenericEnumConverter<Roles>());
                        serOprions.Converters.Add(new GenericEnumConverter<ReportPeriods>());
                        _client = new RestClient(
                            options: options,
                            configureSerialization: s => s.UseNewtonsoftJson(serOprions)
                        );

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

        private void ValidateResponse(RestResponse response)
        {
            if (response.IsSuccessful) { return; }

            if (response.IsSuccessStatusCode)
            {
                var message = "Planar service return success status code but the response content is invalid";
                if (!string.IsNullOrWhiteSpace(response.ErrorMessage))
                {
                    message += $". Inner error message: {response.ErrorMessage}";
                }

                if (response.ErrorException == null)
                {
                    throw new PlanarException(message);
                }

                throw new PlanarException(message, response.ErrorException);
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                if (!string.IsNullOrWhiteSpace(response.Content))
                {
                    PlanarValidationErrors? errorResponse = null;
                    try
                    {
                        errorResponse = System.Text.Json.JsonSerializer.Deserialize<PlanarValidationErrors>(response.Content);
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

                HandleODataErrorResponse(response);
                throw new PlanarValidationException("Planar service return validation errors");
            }

            if (response.StatusCode == HttpStatusCode.Conflict) { throw new PlanarConflictException(response); }
            if (response.StatusCode == HttpStatusCode.Forbidden) { throw new PlanarForbiddenException(response); }
            if (response.StatusCode == HttpStatusCode.RequestTimeout) { throw new PlanarRequestTimeoutException(response); }
            if (response.StatusCode == HttpStatusCode.ServiceUnavailable) { throw new PlanarServiceUnavailableException(response); }
            if (response.StatusCode == HttpStatusCode.Unauthorized) { throw new PlanarUnauthorizedException(response); }
            if (response.StatusCode == HttpStatusCode.TooManyRequests) { throw new PlanarTooManyRequestsException(response); }
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                if (string.IsNullOrWhiteSpace(response.Content))
                {
                    throw new PlanarNotFoundException(response);
                }

                throw new PlanarNotFoundException(response.Content);
            }

            throw new PlanarException(response);
        }

        private static void HandleODataErrorResponse(RestResponse response)
        {
            static string ClearMessage(string message)
            {
                var index = message.IndexOf("on type '");
                if (index < 0) { return message; }
                return message[0..index].ToLower();
            }

            if (response.StatusCode != HttpStatusCode.BadRequest) { return; }
            if (string.IsNullOrWhiteSpace(response.Content)) { return; }
            var token = JToken.Parse(response.Content);
            var message = token["error"]?["innererror"]?["message"]?.ToString();
            if (!string.IsNullOrWhiteSpace(message))
            {
                message = ClearMessage(message);
                throw new PlanarValidationException(message);
            }

            message = token["error"]?["message"]?.ToString();
            if (!string.IsNullOrWhiteSpace(message))
            {
                message = ClearMessage(message);
                throw new PlanarValidationException(message);
            }
        }

        public async Task<TResponse> InvokeAsync<TResponse>(RestRequest request, CancellationToken cancellationToken)
        {
            var response = await Proxy.ExecuteAsync<TResponse>(request, cancellationToken);
            if (await RefreshToken(response, cancellationToken))
            {
                response = await Proxy.ExecuteAsync<TResponse>(request, cancellationToken);
            }

            ValidateResponse(response);

            return response.Data!;
        }

        public async Task<string?> InvokeAsync(RestRequest request, CancellationToken cancellationToken)
        {
            var response = await Proxy.ExecuteAsync(request, cancellationToken);
            if (await RefreshToken(response, cancellationToken))
            {
                response = await Proxy.ExecuteAsync(request, cancellationToken);
            }

            ValidateResponse(response);
            return response.Content;
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
            if (string.IsNullOrEmpty(login.Host)) { throw new PlanarException("Host is mandatory"); }
            if (login.Timeout.HasValue)
            {
                if (login.Timeout == TimeSpan.Zero) { throw new PlanarException("Login timeout must be greater the 0"); }
                Timeout = login.Timeout.Value;
            }

            var schema = GetSchema(login.SecureProtocol);

            var options = new RestClientOptions
            {
                BaseUrl = new UriBuilder(schema, login.Host, login.Port).Uri,
                Timeout = TimeSpan.FromMilliseconds(Convert.ToInt32(Timeout.TotalMilliseconds)),
            };

            var client = new RestClient(options);
            var restRequest = new RestRequest("service/login", Method.Post);
            var body = new { login.Username, login.Password };
            restRequest.AddBody(body);

            var result = await client.ExecuteAsync<LoginResponse>(restRequest, cancellationToken);
            if (result.StatusCode == HttpStatusCode.Conflict)
            {
                Host = login.Host;
                Port = login.Port;
                SecureProtocol = login.SecureProtocol;
                Flush();

                Username = login.Username;
                Password = login.Password;
            }

            if (result.IsSuccessful)
            {
                if (result.Data == null) { throw new PlanarException("The data return from login service is invalid"); }

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