using RestSharp;
using System.Threading.Tasks;
using RestSharp.Serializers.NewtonsoftJson;
using System;

namespace Planar.CLI
{
    internal class RestProxy
    {
        public const string Schema = "http";
        public static string Host { get; set; } = "localhost";
        public static int Port { get; set; } = 2306;

        private static Uri BaseUri
        {
            get
            {
                return new UriBuilder(Schema, Host, Port).Uri;
            }
        }

        public static async Task<RestResponse<TResponse>> Invoke<TResponse>(RestRequest request)
        {
            var client = new RestClient(BaseUri);
            client.UseNewtonsoftJson();
            var response = await client.ExecuteAsync<TResponse>(request);
            return response;
        }

        public static async Task<RestResponse> Invoke(RestRequest request)
        {
            var client = new RestClient(BaseUri);
            client.UseNewtonsoftJson();
            var response = await client.ExecuteAsync(request);
            return response;
        }
    }
}