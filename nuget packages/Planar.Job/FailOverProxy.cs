using CloudNative.CloudEvents;
using CloudNative.CloudEvents.NewtonsoftJson;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Planar.Job
{
    internal sealed class FailOverProxy : IDisposable
    {
        private readonly HttpClient _client;
        private static readonly JsonEventFormatter _formatter = new JsonEventFormatter();

        public FailOverProxy(int port)
        {
            var uri = new Uri($"http://127.0.0.1:{port}");
            var timeout = TimeSpan.FromSeconds(5);
            _client = CreateHttpClient(uri, null, timeout);
        }

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

            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue($"{nameof(Planar)}.{nameof(Job)}.{nameof(FailOverProxy)}"));

            return client;
        }

        private static HttpRequestMessage CreateRequest(string body)
        {
            const string contentType = "application/json";
            const string resource = "job/failover-publish";

            var request = new HttpRequestMessage(HttpMethod.Post, resource);
            var content = new StringContent(body, Encoding.UTF8, contentType);
            request.Content = content;
            if (request.Content.Headers.ContentType != null)
            {
                request.Content.Headers.ContentType.MediaType = contentType;
            }

            return request;
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

        private async Task<HttpResponseMessage> ExecuteRestWithRetryAsync(HttpRequestMessage request)
        {
            const int retryCount = 3;
            var counter = 0;
#if NETSTANDARD2_0
            HttpResponseMessage response = null;
            Exception exception = null;
#else
            HttpResponseMessage? response = null;
            Exception? exception = null;
#endif

            while (counter < retryCount)
            {
                try
                {
                    response = await _client.SendAsync(request);

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

            return response ?? new HttpResponseMessage(System.Net.HttpStatusCode.Conflict);
        }
    }
}