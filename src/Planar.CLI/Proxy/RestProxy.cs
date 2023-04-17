using Planar.API.Common.Entities;
using RestSharp;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.Proxy
{
    internal static class RestProxy
    {
        public static bool SecureProtocol { get; set; }
        public static string Host { get; set; } = "localhost";
        public static int Port { get; set; } = 2306;

        private static RestClient? _client;
        private static readonly object _lock = new();

        public static void Flush()
        {
            lock (_lock)
            {
                _client = null;
            }
        }

        private static RestClient Proxy
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
                            MaxTimeout = 10000,
                        };

                        _client = new RestClient(options);

                        if (!string.IsNullOrEmpty(LoginProxy.Token))
                        {
                            _client.AddDefaultHeader("Authorization", $"Bearer {LoginProxy.Token}");
                        }
                    }

                    return _client;
                }
            }
        }

        private static Uri BaseUri => new UriBuilder(Schema, Host, Port).Uri;

        private static string Schema => GetSchema(SecureProtocol);

        private static async Task<bool> RefreshToken(RestResponse response, CancellationToken cancellationToken)
        {
            if (!LoginProxy.IsAuthorized) { return false; }
            if (response.StatusCode != HttpStatusCode.Unauthorized) { return false; }

            var reloginResponse = await LoginProxy.Relogin(cancellationToken);
            return reloginResponse.IsSuccessful;
        }

        internal static string GetSchema(bool secureProtocol)
        {
            return secureProtocol ? "https" : "http";
        }

        public static async Task<RestResponse<TResponse>> Invoke<TResponse>(RestRequest request, CancellationToken cancellationToken)
        {
            var response = await Proxy.ExecuteAsync<TResponse>(request, cancellationToken);
            if (await RefreshToken(response, cancellationToken))
            {
                response = await Proxy.ExecuteAsync<TResponse>(request, cancellationToken);
            }

            return response;
        }

        public static async Task<RestResponse> Invoke(RestRequest request, CancellationToken cancellationToken)
        {
            var response = await Proxy.ExecuteAsync(request, cancellationToken);
            if (await RefreshToken(response, cancellationToken))
            {
                response = await Proxy.ExecuteAsync(request, cancellationToken);
            }

            return response;
        }
    }
}