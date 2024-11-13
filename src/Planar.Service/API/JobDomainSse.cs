using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.API;

internal class JobDomainSse
{
    private string _fireInstanceId = string.Empty;
    private readonly AutoResetEvent _autoResetEvent = new(false);
    private LogEntity? _log;

    public async Task GetRunningLog(string instanceId, HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_fireInstanceId)) { return; }

        MqttBrokerService.InterceptingPublishAsync += MqttBrokerService_InterceptingPublishAsync;
        try
        {
            await GetRunningLogInner(instanceId, httpContext, cancellationToken);
        }
        finally
        {
            MqttBrokerService.InterceptingPublishAsync -= MqttBrokerService_InterceptingPublishAsync;
        }
    }

    private async Task GetRunningLogInner(string instanceId, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromMinutes(5);
        _fireInstanceId = instanceId;
        httpContext.Response.Headers.Append("Content-Type", "text/event-stream");
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        while (!cts.IsCancellationRequested)
        {
            var signal = _autoResetEvent.WaitOne(TimeSpan.FromMinutes(1));
            if (!signal) { continue; }
            if (_log == null) { continue; }

            await httpContext.Response.WriteAsync($"{_log}\n", cancellationToken: cancellationToken);
            await httpContext.Response.Body.FlushAsync(cancellationToken);

            _log = null;
        }
    }

    private void MqttBrokerService_InterceptingPublishAsync(object? sender, CloudEventArgs e)
    {
        if (e.ClientId != _fireInstanceId) { return; }
        if (e.CloudEvent == null) { return; }
        if (e.CloudEvent.Data == null) { return; }
        if (!Enum.TryParse<MessageBrokerChannels>(e.CloudEvent.Type, ignoreCase: true, out var channel)) { return; }
        if (channel != MessageBrokerChannels.AppendLog) { return; }

        var data = e.CloudEvent.Data;
        var json = data.ToString();
        if (string.IsNullOrWhiteSpace(json)) { return; }
        _log = JsonConvert.DeserializeObject<LogEntity>(json);
        _autoResetEvent.Set();
    }
}