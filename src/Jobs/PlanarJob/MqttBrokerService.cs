﻿using CloudNative.CloudEvents;
using CloudNative.CloudEvents.NewtonsoftJson;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Server;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planar;

public class MqttBrokerService(ILogger<MqttBrokerService> logger) : IHostedService
{
    private const int _port = 206;
    private static readonly JsonEventFormatter _formatter = new();
    private MqttServer _mqttServer = null!;

    internal static event EventHandler<CloudEventArgs>? InterceptingPublishAsync;

    public static void OnInterceptingPublishAsync(CloudEvent cloudEvent)
    {
        if (InterceptingPublishAsync != null)
        {
            var args = new CloudEventArgs(cloudEvent, cloudEvent.Subject ?? string.Empty);
            InterceptingPublishAsync(null, args);
        }
    }

    private static void OnInterceptingPublishAsync(CloudEvent cloudEvent, InterceptingPublishEventArgs arg)
    {
        if (InterceptingPublishAsync != null)
        {
            var cloudEventArgs = new CloudEventArgs(cloudEvent, arg.ClientId);
            InterceptingPublishAsync(null, cloudEventArgs);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
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
            logger.LogInformation("Initialize: {Operation}", "Starting MQTT Broker Service...");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Initialize: Fail To {Operation}", "Start MQTT Broker Service");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_mqttServer == null) { return; }
        SafeHandle(() => _mqttServer.ClientConnectedAsync -= ClientConnected);
        SafeHandle(() => _mqttServer.InterceptingPublishAsync -= InterceptingPublish);
        SafeHandle(() => _mqttServer.StartedAsync -= StartedAsync);
        await SafeHandleAsync(_mqttServer.StopAsync);
        SafeHandle(_mqttServer.Dispose);
    }

    private static void SafeHandle(Action action)
    {
        try
        {
            action.Invoke();
        }
        catch
        {
            // *** DO NOTHING ***
        }
    }

    private static async Task SafeHandleAsync(Func<Task> func)
    {
        try
        {
            await func.Invoke();
        }
        catch
        {
            // *** DO NOTHING ***
        }
    }

    private async Task ClientConnected(ClientConnectedEventArgs arg)
    {
        SafeHandle(() =>
        logger.LogDebug("New MQTT connection: ClientId = {ClientId}, Endpoint = {Cndpoint}", arg.ClientId, arg.Endpoint));

        await Task.CompletedTask;
    }

    private async Task InterceptingPublish(InterceptingPublishEventArgs arg)
    {
        try
        {
            var cloudEvent = arg.ApplicationMessage.ToCloudEvent(_formatter);
            OnInterceptingPublishAsync(cloudEvent, arg);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "fail to handle MQTT published message");
        }

        await Task.CompletedTask;
    }

    private async Task StartedAsync(EventArgs arg)
    {
        logger.LogInformation("Initialize: {Operation}", "MQTT Broker Service started");
        await Task.CompletedTask;
    }

    private async Task StoppedAsync(EventArgs arg)
    {
        logger.LogWarning("stopped MQTT Broker Service");
        await Task.CompletedTask;
    }
}