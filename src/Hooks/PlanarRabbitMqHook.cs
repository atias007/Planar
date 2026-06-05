using CloudNative.CloudEvents;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Hook;
using Planar.Hooks.Serialize;
using System.Net.Mime;

namespace Planar.Hooks;

public sealed class PlanarRabbitMqHook(ILogger<PlanarRabbitMqHook> logger, RabbitMqFactory rabbitMqFactory) : BaseSystemHook(logger)
{
    public override string Name => "Planar.RabbitMq";

    public override string Description =>
"""
This hook sends messages to a RabbitMQ exchange.
You can find the default RabbitMQ settings in appsettings.yml (Data folder of Planar).
To use different settings per group, you can set one of the 'AdditionalField' of monitor group to the following value:
----------------------------------------------
  rabbitmq-exchange:<your-exchange-name>
  rabbitmq-routing-key:<your-routing-key>
----------------------------------------------
*** only one exchange and/or one routing key is allowed per group ***
""";

    public async override Task Handle(IMonitorDetails monitorDetails)
    {
        await HandleInner(monitorDetails, monitorDetails.FireInstanceId);
    }

    public async override Task HandleSystem(IMonitorSystemDetails monitorDetails)
    {
        await HandleInner(monitorDetails);
    }

    public async Task HandleInner<T>(T details, string? fireInstanceId = null)
        where T : IMonitor
    {
        var exchange = GetRabbitParameter(details, "rabbitmq-exchange") ?? string.Empty;
        var routing = GetRabbitParameter(details, "rabbitmq-routing-key") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(exchange) && string.IsNullOrWhiteSpace(routing)) { return; }

        try
        {
#pragma warning disable S1075 // URIs should not be hardcoded
            var body = new CloudEvent
            {
                Id = Guid.NewGuid().ToString("N"),
                Time = DateTimeOffset.Now,
                Subject = Name,
                Data = details,
                DataContentType = MediaTypeNames.Application.Json,
                Source = new Uri("https://www.planar.me"),
                Type = typeof(T).Name
            };
#pragma warning restore S1075 // URIs should not be hardcoded
#pragma warning restore S1075 // URIs should not be hardcoded

            body.SetAttributeFromString("version", "1.0.0");
            var json = CoreSerializer.SerializeCloudEvent(body);
            await rabbitMqFactory.PublishAsync(exchange, routing, fireInstanceId, command: "Alert", body: json);
        }
        catch (Exception ex)
        {
            throw new PlanarHookException($"fail to invoke '{details.MonitorTitle}' with '{Name}' hook. message: {ex.Message}", ex);
        }
    }

    private string? GetRabbitParameter(IMonitor monitor, string name)
    {
        foreach (var group in monitor.Groups)
        {
            var prm = GetParameter(name, group);
            if (!string.IsNullOrWhiteSpace(prm))
            {
                return prm;
            }
        }

        if (!string.IsNullOrWhiteSpace(AppSettings.Hooks.RabbitMq.Exchange))
        {
            return AppSettings.Hooks.RabbitMq.Exchange;
        }

        LogError($"RabbitMq.Hook: {name} is null or empty");
        return null;
    }
}