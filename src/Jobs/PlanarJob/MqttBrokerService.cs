using CloudNative.CloudEvents;
using CloudNative.CloudEvents.NewtonsoftJson;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Server;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planar
{
    public class MqttBrokerService : IHostedService
    {
        private const int _port = 206;
        private MqttServer _mqttServer = null!;
        private static readonly JsonEventFormatter _formatter = new();
        private readonly ILogger<MqttBrokerService> _logger;

        public static event EventHandler<CloudEventArgs>? InterceptingPublishAsync;

        public MqttBrokerService(ILogger<MqttBrokerService> logger)
        {
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var options = new MqttServerOptionsBuilder()
                .WithDefaultEndpoint()
                .WithDefaultEndpointPort(_port)
                .WithDefaultCommunicationTimeout(TimeSpan.FromSeconds(5))
                .WithKeepAlive()
                .WithPersistentSessions()
                .WithTcpKeepAliveInterval(3)
                .WithTcpKeepAliveRetryCount(3)
                .Build();

            _mqttServer = new MqttFactory().CreateMqttServer(options);
            _mqttServer.ClientConnectedAsync += ClientConnected;
            _mqttServer.InterceptingPublishAsync += InterceptingPublish;
            _mqttServer.StartedAsync += StartedAsync;
            _mqttServer.StoppedAsync += StoppedAsync;
            await _mqttServer.StartAsync();
        }

        private async Task StoppedAsync(EventArgs arg)
        {
            // TODO: add log info
            await Task.CompletedTask;
        }

        private async Task StartedAsync(EventArgs arg)
        {
            // TODO: add log info
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // TODO: try/catch for each row
            if (_mqttServer == null) { return; }
            _mqttServer.ClientConnectedAsync -= ClientConnected;
            _mqttServer.InterceptingPublishAsync -= InterceptingPublish;
            _mqttServer.StartedAsync -= StartedAsync;
            await _mqttServer.StopAsync();
            _mqttServer.Dispose();
        }

        private void OnInterceptingPublishAsync(CloudEvent cloudEvent, InterceptingPublishEventArgs arg)
        {
            // TODO: try/catch
            try
            {
                if (InterceptingPublishAsync != null)
                {
                    var cloudEventArgs = new CloudEventArgs(cloudEvent, arg.ClientId);
                    InterceptingPublishAsync(null, cloudEventArgs);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task InterceptingPublish(InterceptingPublishEventArgs arg)
        {
            // TODO: try/catch
            try
            {
                var cloudEvent = arg.ApplicationMessage.ToCloudEvent(_formatter);
                OnInterceptingPublishAsync(cloudEvent, arg);
            }
            catch (Exception ex)
            {
                throw;
            }

            await Task.CompletedTask;
        }

        private async Task ClientConnected(ClientConnectedEventArgs arg)
        {
            // TODO: try/catch
            // TODO: add log info
            // _logger.LogDebug("New MQTT connection: ClientId = {ClientId}, Endpoint = {Cndpoint}", arg.ClientId, arg.Endpoint)
            await Task.CompletedTask;
        }
    }
}