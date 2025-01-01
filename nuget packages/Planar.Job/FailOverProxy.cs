using RestSharp;
using System;
using System.Threading.Tasks;

namespace Planar.Job
{
    internal sealed class FailOverProxy : IDisposable
    {
        private readonly IRestClient _client;
        private readonly RestRequest _restRequest;
        private readonly string _fireInstanceId;

        public FailOverProxy(int port, string fireInstanceId)
        {
            _fireInstanceId = fireInstanceId;

            var options = new RestClientOptions
            {
                Timeout = TimeSpan.FromSeconds(10),
                BaseUrl = new Uri($"http://127.0.0.1:{port}"),
                UserAgent = $"{nameof(Planar)}.{nameof(Job)}.{nameof(FailOverProxy)}"
            };

            _client = new RestClient(options);
            _restRequest = new RestRequest
            {
                Resource = "job/failover-publish"
            };
        }

        public async Task PublishAsync(MessageBrokerChannels channel)
        {
            var cloudEvent = MqttClient.CreateCloudEvent(channel);
            var body = new { ClientId = _fireInstanceId, CloudEvent = cloudEvent };
            _restRequest.AddBody(body);
            await _client.ExecuteAsync(_restRequest);
        }

        public async Task PublishAsync(MessageBrokerChannels channel, object message)
        {
            var cloudEvent = MqttClient.CreateCloudEvent(channel, message);
            var body = new { ClientId = _fireInstanceId, CloudEvent = cloudEvent };
            _restRequest.AddBody(body);
            await _client.ExecuteAsync(_restRequest);
        }

        public async Task PingAsync()
        {
            var cloudEvent = MqttClient.CreateCloudEvent(MessageBrokerChannels.HealthCheck);
            var body = new { ClientId = _fireInstanceId, CloudEvent = cloudEvent };
            _restRequest.AddBody(body);
            var response = await _client.ExecuteAsync(_restRequest);
            if (!response.IsSuccessStatusCode)
            {
                throw new PlanarJobException($"Fail to ping failover proxy. Server status: {response.StatusCode}");
            }
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}