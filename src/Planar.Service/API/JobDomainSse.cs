using CommonJob;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Planar.Common;
using Planar.Service.Exceptions;
using Planar.Service.General;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.API;

public class JobDomainSse(IServiceProvider serviceProvider) : BaseBL<JobDomainSse>(serviceProvider)
{
    private string _fireInstanceId = string.Empty;
    private bool _finish;
    private readonly AutoResetEvent _autoResetEvent = new(false);
    private LogEntity? _log;

    public async Task GetRunningLog(string instanceId, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_fireInstanceId)) { return; }
        var context = _serviceProvider.GetRequiredService<IHttpContextAccessor>();
        if (context.HttpContext == null) { return; }
        await ValidateRunning(instanceId);

        JobLogBroker.InterceptingLogMessage += MqttBrokerService_InterceptingLogMessage;
        PlanarBrokerService.InterceptingMessage += PlanarBrokerService_InterceptingMessage;
        try
        {
            await GetRunningLogInner(instanceId, context.HttpContext, cancellationToken);
        }
        finally
        {
            JobLogBroker.InterceptingLogMessage -= MqttBrokerService_InterceptingLogMessage;
            PlanarBrokerService.InterceptingMessage -= PlanarBrokerService_InterceptingMessage;
        }
    }

    private void PlanarBrokerService_InterceptingMessage(object? sender, InterceptMessageEventArgs e)
    {
        if (e.MonitorEvent != MonitorEvents.ExecutionEnd) { return; }
        if (e.ExecutionContext.FireInstanceId != _fireInstanceId) { return; }
        Thread.Sleep(2000);
        _finish = true;
        _autoResetEvent.Set();
    }

    private async Task GetRunningLogInner(string instanceId, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromMinutes(5);
        _fireInstanceId = instanceId;
        httpContext.Response.Headers.Append("Content-Type", "text/event-stream");
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);
        cts.Token.Register(() =>
        {
            _log = null;
            _autoResetEvent.Set();
        });

        while (!cts.IsCancellationRequested)
        {
            var signal = _autoResetEvent.WaitOne(timeout);
            if (_finish) { break; }
            if (!signal) { continue; }
            if (_log == null) { continue; }
            var text = _log.ToString();
            if (string.IsNullOrWhiteSpace(text)) { continue; }

            await httpContext.Response.WriteAsync($"{text}\n", cancellationToken: cancellationToken);
            await httpContext.Response.Body.FlushAsync(cancellationToken);

            _log = null;
        }
    }

    private void MqttBrokerService_InterceptingLogMessage(object? sender, LogEntityEventArgs e)
    {
        if (e.FireInstanceId != _fireInstanceId) { return; }
        _log = e.Log;
        _autoResetEvent.Set();
    }

    private async Task ValidateRunning(string instanceId)
    {
        var result = await SchedulerUtil.GetRunningJob(instanceId);
        if (result == null && AppSettings.Cluster.Clustering)
        {
            result = await ClusterUtil.GetRunningJob(instanceId);
        }

        if (result == null)
        {
            throw new RestNotFoundException();
        }
    }
}