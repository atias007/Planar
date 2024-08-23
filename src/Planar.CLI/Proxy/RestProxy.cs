using Core.JsonConvertor;
using Newtonsoft.Json;
using Planar.CLI.DataProtect;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.Proxy
{
    internal static class RestProxy
    {
        public static bool SecureProtocol { get; set; }
        public static string Host { get; set; } = ConnectUtil.DefaultHost;
        public static int Port { get; set; } = ConnectUtil.GetDefaultPort();

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
                if (_client != null) { return _client; }
                lock (_lock)
                {
                    if (_client != null) { return _client; }
                    var options = new RestClientOptions
                    {
                        BaseUrl = BaseUri,
                        Timeout = TimeSpan.FromMilliseconds(60_000),
                    };

                    var serOprions = new JsonSerializerSettings();
                    serOprions.Converters.Add(new NewtonsoftTimeSpanConverter());
                    serOprions.Converters.Add(new NewtonsoftNullableTimeSpanConverter());

                    _client = new RestClient(
                        options: options,
                        configureSerialization: s => s.UseNewtonsoftJson(serOprions)
                        );

                    if (!string.IsNullOrEmpty(LoginProxy.Token))
                    {
                        _client.AddDefaultHeader("Authorization", $"Bearer {LoginProxy.Token}");
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

        private static void SetDefaultRequestTimeout(RestRequest request)
        {
            if (request.Timeout == TimeSpan.Zero)
            {
                request.Timeout = TimeSpan.FromMilliseconds(10_000);
            }
        }

        public static async Task<RestResponse<TResponse>> Invoke<TResponse>(RestRequest request, CancellationToken cancellationToken)
        {
            SetDefaultRequestTimeout(request);
            var response = await Proxy.ExecuteAsync<TResponse>(request, cancellationToken);
            if (await RefreshToken(response, cancellationToken))
            {
                response = await Proxy.ExecuteAsync<TResponse>(request, cancellationToken);
            }

            return response;
        }

        public static async Task<RestResponse> Invoke(RestRequest request, CancellationToken cancellationToken)
        {
            SetDefaultRequestTimeout(request);
            var response = await Proxy.ExecuteAsync(request, cancellationToken);
            if (await RefreshToken(response, cancellationToken))
            {
                response = await Proxy.ExecuteAsync(request, cancellationToken);
            }

            return response;
        }
    }
}