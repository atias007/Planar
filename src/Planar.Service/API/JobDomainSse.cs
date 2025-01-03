using CommonJob;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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
    private static readonly TimeSpan _sseTimeout = TimeSpan.FromMinutes(5);

    public async Task GetRunningLog(string instanceId, CancellationToken cancellationToken)
    {
        var httpContext = ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        if (httpContext.HttpContext == null) { return; }

        var context = new CommonSseContext(httpContext.HttpContext);
        await GetRunningLog(instanceId, context, cancellationToken);
    }

    public async Task GetRunningLog(string instanceId, CommonSseContext context, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_fireInstanceId)) { return; }

        try
        {
            await ValidateRunning(instanceId);
        }
        catch (RestNotFoundException)
        {
            await GetRunningLogFromClusterNode(instanceId, context, cancellationToken);
            return;
        }

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
            await GetRunningLogInner(instanceId, context, cancellationToken);
        }
        finally
        {
            JobLogBroker.InterceptingLogMessage -= LogJobBroker_InterceptingLogMessage;
            PlanarBrokerService.InterceptingMessage -= PlanarBrokerService_InterceptingMessage;
            _runningInstanceIds.Remove(instanceId);
        }
    }

    private async Task GetRunningLogFromClusterNode(string instanceId, CommonSseContext context, CancellationToken cancellationToken)
    {
        if (!AppSettings.Cluster.Clustering) { return; }

        var reader = await ClusterUtil.GetRunningLog(instanceId);
        if (reader == null) { return; }

        context.Initialize();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_sseTimeout);

        while (await reader.MoveNext(cancellationToken))
        {
            await context.WriteResponse(reader.Current, cts.Token);
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

    private async Task GetRunningLogInner(string instanceId, CommonSseContext context, CancellationToken cancellationToken)
    {
        _fireInstanceId = instanceId;

        context.Initialize();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_sseTimeout);
        cts.Token.Register(() => _autoResetEvent.Set());

        // write (up to 50) past logs from queue
        await PrintLogQueue(context, cts);

        // write live logs
        while (!cts.IsCancellationRequested)
        {
            var signal = _autoResetEvent.WaitOne(_sseTimeout);
            if (_finish) { break; }
            if (!signal) { break; } // timeout
            await PrintLogQueue(context, cts);
        }
    }

    private async Task PrintLogQueue(CommonSseContext context, CancellationTokenSource cts)
    {
        // write (up to 50) past logs from queue
        while (!cts.IsCancellationRequested)
        {
            if (_finish) { break; }
            var log = LogQueueFactory.Instance.Dequeue(_fireInstanceId);
            if (log == null) { break; }
            await context.WriteResponse(log, cts.Token);
        }
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