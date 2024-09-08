using Microsoft.AspNetCore.Http;
using Planar.API.Common.Entities;
using Planar.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.API;

public partial class JobDomain
{
    private struct ServerEventData
    {
        public int TotalRunningInstances { get; set; }
        public TimeSpan? EstimatedEndTime { get; set; }
    }

    public async Task Wait(JobWaitRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await WaitInner(request, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            var context = Resolve<IHttpContextAccessor>().HttpContext;
            ArgumentNullException.ThrowIfNull(context);
            await context.Response.Body.FlushAsync();
        }
    }

    private async Task WaitInner(JobWaitRequest request, CancellationToken cancellationToken)
    {
        const int refreshRate = 2000; // # 2 seconds
        const int maxWaitTime = 30 * 60 * 1000; // # 30 minutes

        // Get http context
        var context = Resolve<IHttpContextAccessor>().HttpContext;
        ArgumentNullException.ThrowIfNull(context);

        // Server send event headers
        context.Response.Headers.Append("Content-Type", "text/event-stream");

        // Create timeout token
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(maxWaitTime);
        var key = string.Empty;
        bool first = true;
        while (!cts.IsCancellationRequested)
        {
            var items = await GetRunningInner(request);
            if(first && items.Count == 0)
            {
                await Task.Delay(2000, cancellationToken);
                items = await GetRunningInner(request);
            }

            first = false;
            if (items.Count == 0) { return; }
            var newKey = GetRunningHashKey(items);
            if (key == newKey) { continue; }
            key = newKey;
            await SendServerEvent(context, "message", items, cancellationToken);
            await Task.Delay(refreshRate, cancellationToken);
        }

        if (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            var items = await GetRunningInner(request);
            if (items.Count == 0) { return; }
            await SendServerEvent(context, "timeout", items, cancellationToken);
        }
    }

    private async Task<List<RunningJobDetails>> GetRunningInner(JobWaitRequest request)
    {
        var items = await GetRunning();
        request.Trim();
        if (request.Group.HasValue())
        {
            var result = items.Where(x => string.Equals(x.Group, request.Group, StringComparison.OrdinalIgnoreCase)).ToList();
            return result;
        }

        if (request.Id.HasValue())
        {
            var result = items.Where(x => x.EqualsId(request.Id)).ToList();
            return result;
        }

        return items;
    }

    private static async Task SendServerEvent(HttpContext context, string @event, IEnumerable<RunningJobDetails> items, CancellationToken cancellationToken)
    {
        const string data = "data: ";
        const string footer = "\n\n";
        var eventData = new ServerEventData
        {
            EstimatedEndTime = GetEstimatedEndTime(items),
            TotalRunningInstances = items.Count()
        };

        await context.Response.WriteAsync($"event: {@event}\n", cancellationToken);
        await context.Response.WriteAsync(data, cancellationToken);
        await JsonSerializer.SerializeAsync(context.Response.Body, eventData, cancellationToken: cancellationToken);
        await context.Response.WriteAsync($"\nid: {DateTime.Now.Ticks}", cancellationToken);
        await context.Response.WriteAsync(footer, cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);
    }

    private static string GetRunningHashKey(IEnumerable<RunningJobDetails> items)
    {
        var est = GetEstimatedEndTime(items);
        var cnt = items.Count();
        return $"{cnt}_{est:hh\\:mm\\:ss}";
    }

    private static TimeSpan? GetEstimatedEndTime(IEnumerable<RunningJobDetails> items)
    {
        var total = items.Where(x => x.EstimatedEndTime.HasValue).ToList();
        if (total.Count == 0) { return null; }
        return total.Max(x => x.EstimatedEndTime);
    }
}