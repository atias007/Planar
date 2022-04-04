using RestSharp;
using System;
using System.Threading.Tasks;

namespace Planner.CLI
{
    internal class RestProxy
    {
        private const string baseUrl = "http://localhost:2306";

        public static async Task<TResponse> Invoke<TResponse>(RestRequest request)
        {
            var client = new RestClient(baseUrl);
            var response = await client.ExecuteAsync<TResponse>(request);
            if (response.IsSuccessful)
            {
                return response.Data;
            }
            else
            {
                throw new Exception("To be implemented");
            }
        }

        public static async Task Invoke(RestRequest request)
        {
            var client = new RestClient(baseUrl);
            var response = await client.ExecuteAsync(request);
            if (response.IsSuccessful == false)

            {
                throw new Exception("To be implemented");
            }
        }
    }
}