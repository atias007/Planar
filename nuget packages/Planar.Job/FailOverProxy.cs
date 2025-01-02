using CloudNative.CloudEvents;
using CloudNative.CloudEvents.NewtonsoftJson;
using RestSharp;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Planar.Job
{
    internal sealed class FailOverProxy : IDisposable
    {
        private readonly IRestClient _client;
        private readonly string _fireInstanceId;
        private static readonly JsonEventFormatter _formatter = new JsonEventFormatter();

        public FailOverProxy(int port, string fireInstanceId)
        {
            _fireInstanceId = fireInstanceId;

            var options = new RestClientOptions
            {
                Timeout = TimeSpan.FromSeconds(5),
                BaseUrl = new Uri($"http://127.0.0.1:{port}"),
                UserAgent = $"{nameof(Planar)}.{nameof(Job)}.{nameof(FailOverProxy)}"
            };

            _client = new RestClient(options);
        }

        private static RestRequest CreateRequest(string body)
        {
            var restRequest = new RestRequest
            {
                Resource = "job/failover-publish",
                Method = Method.Post
            };

            restRequest.AddJsonBody(body);
            return restRequest;
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        public async Task PingAsync(CloudEvent cloudEvent)
        {
            var bytes = _formatter.EncodeStructuredModeMessage(cloudEvent, out _);
            var json = Encoding.UTF8.GetString(bytes.ToArray());
            var restRequest = CreateRequest(json);
            var response = await ExecuteRestWithRetryAsync(restRequest);
            if (!response.IsSuccessStatusCode)
            {
                throw new PlanarJobException($"Fail to ping failover proxy. Server status: {response.StatusCode}");
            }
        }

        public async Task PublishAsync(CloudEvent cloudEvent)
        {
            var bytes = _formatter.EncodeStructuredModeMessage(cloudEvent, out _);
            var json = Encoding.UTF8.GetString(bytes.ToArray());
            var restRequest = CreateRequest(json);
            await ExecuteRestWithRetryAsync(restRequest);
        }

        private async Task<RestResponse> ExecuteRestWithRetryAsync(RestRequest request)
        {
            const int retryCount = 3;
            var counter = 0;
            RestResponse? response = null;
            Exception? exception = null;
            while (counter < retryCount)
            {
                try
                {
                    response = await _client.ExecuteAsync(request);
                    if (response.IsSuccessStatusCode) { return response; }
                    if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500) { return response; }
                }
                catch (Exception ex)
                {
                    // ** do nothing **
                    exception = ex;
                }

                await Task.Delay(1000);
                counter++;
            }

            if (exception != null) { throw exception; }

            return response ?? new RestResponse(request)
            {
                IsSuccessStatusCode = false,
                StatusCode = System.Net.HttpStatusCode.Conflict
            };
        }
    }
}