using CommonJob;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Planar.Common;
using Planar.Service.Exceptions;
using Planar.Service.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.API;

public class JobDomainSse(IServiceProvider serviceProvider) : BaseBL<JobDomainSse>(serviceProvider)
{
    private string _fireInstanceId = string.Empty;
    private bool _finish;
    private readonly AutoResetEvent _autoResetEvent = new(false);
    private static readonly HashSet<string> _runningInstanceIds = [];

    public async Task GetRunningLog(string instanceId, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_fireInstanceId)) { return; }

        var context = _serviceProvider.GetRequiredService<IHttpContextAccessor>();
        if (context.HttpContext == null) { return; }
        await ValidateRunning(instanceId);

        lock (_runningInstanceIds)
        {
            if (_runningInstanceIds.Contains(instanceId))
            {
                throw new RestValidationException("instanceId", "action is already running with the same instance id");
            }

            _runningInstanceIds.Add(instanceId);
        }

        JobLogBroker.InterceptingLogMessage += LogJobBroker_InterceptingLogMessage;
        PlanarBrokerService.InterceptingMessage += PlanarBrokerService_InterceptingMessage;
        try
        {
            await GetRunningLogInner(instanceId, context.HttpContext, cancellationToken);
        }
        finally
        {
            JobLogBroker.InterceptingLogMessage -= LogJobBroker_InterceptingLogMessage;
            PlanarBrokerService.InterceptingMessage -= PlanarBrokerService_InterceptingMessage;
            _runningInstanceIds.Remove(instanceId);
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
        cts.Token.Register(() => _autoResetEvent.Set());

        // write (up to 50) past logs from queue
        await PrintLogQueue(httpContext, cts);

        // write live logs
        while (!cts.IsCancellationRequested)
        {
            var signal = _autoResetEvent.WaitOne(timeout);
            if (_finish) { break; }
            if (!signal) { break; } // timeout
            await PrintLogQueue(httpContext, cts);
        }
    }

    private async Task PrintLogQueue(HttpContext httpContext, CancellationTokenSource cts)
    {
        // write (up to 50) past logs from queue
        while (!cts.IsCancellationRequested)
        {
            if (_finish) { break; }
            var log = LogQueueFactory.Instance.Dequeue(_fireInstanceId);
            if (log == null) { break; }
            await WriteSseResponse(httpContext, log, cts.Token);
        }
    }

    // write log to http response
    private static async Task WriteSseResponse(HttpContext httpContext, LogEntity log, CancellationToken cancellationToken)
    {
        var text = log.ToString();
        if (string.IsNullOrWhiteSpace(text)) { return; }

        await httpContext.Response.WriteAsync($"{text}\n", cancellationToken: cancellationToken);
        await httpContext.Response.Body.FlushAsync(cancellationToken);
    }

    private void LogJobBroker_InterceptingLogMessage(object? sender, LogEntityEventArgs e)
    {
        if (e.FireInstanceId != _fireInstanceId) { return; }
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

        var localRunnings = await Scheduler.GetCurrentlyExecutingJobs();
        _ = localRunnings.FirstOrDefault(x => x.FireInstanceId == instanceId)
            ?? throw new RestNotFoundException();
    }
}