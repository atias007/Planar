using RestSharp;
using System.Threading.Tasks;
using RestSharp.Serializers.NewtonsoftJson;
using System;

namespace Planar.CLI
{
    internal class RestProxy
    {
        public static string Schema { get; set; } = "http";
        public static string Host { get; set; } = "localhost";
        public static int Port { get; set; } = 2306;

        private static RestClient _client;
        private static object _lock = new object();

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
                            MaxTimeout = 10000
                        };

                        _client = new RestClient(options);
                        _client.UseNewtonsoftJson();
                    }

                    return _client;
                }
            }
        }

        private static Uri BaseUri
        {
            get
            {
                return new UriBuilder(Schema, Host, Port).Uri;
            }
        }

        public static async Task<RestResponse<TResponse>> Invoke<TResponse>(RestRequest request)
        {
            var response = await Proxy.ExecuteAsync<TResponse>(request);
            return response;
        }

        public static async Task<RestResponse> Invoke(RestRequest request)
        {
            var response = await Proxy.ExecuteAsync(request);
            return response;
        }
    }
}