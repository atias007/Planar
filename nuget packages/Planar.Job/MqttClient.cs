﻿using CloudNative.CloudEvents;
using CloudNative.CloudEvents.NewtonsoftJson;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using System;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

namespace Planar
{
    internal static class MqttClient
    {
        public const string Source = "http://planar.me";

        private const int _autoReconnectDelay = 1;

        private const string _host = "127.0.0.1";

        private const int _keepAlivePeriod = 1;

        private const int _port = 206;

        private const int _timeout = 12;

        private static readonly JsonEventFormatter _formatter = new JsonEventFormatter();

        private static string _id = "none";

        private static IManagedMqttClient _mqttClient = null!;

        public static event EventHandler? Connected;

        public static bool IsConnected => _mqttClient.IsConnected;

        public static CloudEvent CreateCloudEvent(MessageBrokerChannels channel)
        {
            return CreateCloudEvent(channel, string.Empty);
        }

        public static CloudEvent CreateCloudEvent(MessageBrokerChannels channel, object message)
        {
            var cloudEvent = new CloudEvent
            {
                Id = Convert.ToString((int)channel),
                Type = channel.ToString(),
                Time = DateTimeOffset.UtcNow,
                DataContentType = MediaTypeNames.Application.Json,
                Data = message,
                Source = new Uri(Source)
            };

            return cloudEvent;
        }

        public static async Task PingAsync()
        {
            if (_mqttClient == null) { return; }

            await _mqttClient.PingAsync();
        }

        public static async Task PublishAsync(MessageBrokerChannels channel)
        {
            var cloudEvent = CreateCloudEvent(channel, string.Empty);
            await PublishInnerAsync(cloudEvent);
        }

        public static async Task PublishAsync(MessageBrokerChannels channel, object message)
        {
            var cloudEvent = CreateCloudEvent(channel, message);
            await PublishInnerAsync(cloudEvent);
        }

        public static async Task StartAsync(string id, int port)
        {
            _id = id;
            var mqttPort = port == 0 ? _port : port;
            var clientOptions = new MqttClientOptionsBuilder()
                .WithTimeout(TimeSpan.FromSeconds(_timeout))
                .WithClientId(id)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(_keepAlivePeriod))
                .WithTcpServer(_host, mqttPort)
                .Build();

            var options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(_autoReconnectDelay))
                .WithClientOptions(clientOptions)
                .Build();

            _mqttClient = new MqttFactory().CreateManagedMqttClient();
            _mqttClient.ConnectedAsync += ConnectedAsync;
            _mqttClient.DisconnectedAsync += DisconnectedAsync;
            _mqttClient.ConnectingFailedAsync += ConnectingFailedAsync;
            await _mqttClient.StartAsync(options);
        }

        public static async Task StopAsync()
        {
            if (_mqttClient == null) { return; }

            SpinWait.SpinUntil(() => _mqttClient.PendingApplicationMessagesCount == 0, TimeSpan.FromSeconds(10));

            _mqttClient.ConnectedAsync -= ConnectedAsync;
            _mqttClient.DisconnectedAsync -= DisconnectedAsync;
            _mqttClient.ConnectingFailedAsync -= ConnectingFailedAsync;

            await _mqttClient.StopAsync(true);
            _mqttClient?.Dispose();
        }

        private static async Task ConnectedAsync(MqttClientConnectedEventArgs arg)
        {
            _ = OnConnected();
            var log = new LogEntity { Level = LogLevel.Debug, Message = "successfully connected to broker" };
            await Console.Out.WriteLineAsync(log.ToString());
        }

        private static async Task ConnectingFailedAsync(ConnectingFailedEventArgs arg)
        {
            var log = new LogEntity { Level = LogLevel.Critical, Message = $"couldn't connect to mqtt broker! (port {_port})" };
            await Console.Error.WriteLineAsync(log.ToString());

            log = new LogEntity { Level = LogLevel.Critical, Message = arg.Exception.ToString() };
            await Console.Error.WriteLineAsync(log.ToString());
        }

        private static async Task DisconnectedAsync(MqttClientDisconnectedEventArgs arg)
        {
            var log = new LogEntity { Level = LogLevel.Error, Message = "successfully disconnected from broker" };
            await Console.Out.WriteLineAsync(log.ToString());
        }

        private static Task OnConnected()
        {
            return Task.Run(() => Connected?.Invoke(null, EventArgs.Empty));
        }

        private static async Task PublishInnerAsync(CloudEvent cloudEvent)
        {
            if (_mqttClient == null) { return; }
            var mqttMessage = cloudEvent.ToMqttApplicationMessage(ContentMode.Structured, _formatter, _id);
            mqttMessage.QualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce;
            await _mqttClient.EnqueueAsync(mqttMessage);
        }
    }
}