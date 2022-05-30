using RestSharp;
using System.Threading.Tasks;

namespace Planar.CLI
{
    internal class RestProxy
    {
        private const string baseUrl = "http://localhost:2306";

        public static async Task<RestResponse<TResponse>> Invoke<TResponse>(RestRequest request)
        {
            var client = new RestClient(baseUrl);
            var response = await client.ExecuteAsync<TResponse>(request);
            return response;
        }

        public static async Task<RestResponse> Invoke(RestRequest request)
        {
            var client = new RestClient(baseUrl);
            var response = await client.ExecuteAsync(request);
            return response;
        }
    }
}