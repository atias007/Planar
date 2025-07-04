﻿using Planar.Common;
using Planar.Service.Model;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Services;

internal static class MonitorServiceCache
{
    private static readonly TimeSpan _cacheSpan = TimeSpan.FromMinutes(60);
    private static DateTimeOffset _lastUpdate = DateTimeOffset.MinValue;
    private static readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    private static IReadOnlyList<MonitorAction>? _cache;

    public static bool IsCacheValid => _cache != null && DateTimeOffset.UtcNow - _lastUpdate < _cacheSpan;

    public static async Task SetCache(IEnumerable<MonitorAction> cache)
    {
        await _semaphoreSlim.WaitAsync();
        try
        {
            _cache = [.. cache];
        }
        finally
        {
            _semaphoreSlim.Release();
        }

        _lastUpdate = DateTimeOffset.UtcNow;
    }

    public static async Task<IEnumerable<MonitorAction>> GetMonitorActions(MonitorEvents @event, IJobExecutionContext context)
    {
        var key = context.JobDetail.Key;
        var data1 = await GetMonitorDataByEvent((int)@event);
        var data2 = await GetMonitorDataByGroup((int)@event, key.Group);
        var data3 = await GetMonitorDataByJob((int)@event, key.Group, key.Name);

        var result = data1
            .Union(data2)
            .Union(data3)
            .Distinct()
            .ToList();

        return result;
    }

    public static async Task<IEnumerable<MonitorAction>> GetMonitorActions(MonitorEvents @event)
    {
        var result = await GetMonitorDataByEvent((int)@event);
        return result;
    }

    private static async Task<IEnumerable<MonitorAction>> GetMonitorDataByEvent(int @event)
    {
        ArgumentNullException.ThrowIfNull(_cache);

        await _semaphoreSlim.WaitAsync();
        try
        {
            var data = _cache
                .Where(m =>
                    m.EventId == @event &&
                    string.IsNullOrEmpty(m.JobGroup) &&
                    string.IsNullOrEmpty(m.JobName));
            return data;
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    private static async Task<IEnumerable<MonitorAction>> GetMonitorDataByGroup(int @event, string jobGroup)
    {
        ArgumentNullException.ThrowIfNull(_cache);

        await _semaphoreSlim.WaitAsync();
        try
        {
            var data = _cache
              .Where(m =>
                  m.EventId == @event &&
                  m.JobGroup != null && m.JobGroup.Equals(jobGroup, StringComparison.CurrentCultureIgnoreCase) &&
                  string.IsNullOrEmpty(m.JobName));

            return data;
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    private static async Task<IEnumerable<MonitorAction>> GetMonitorDataByJob(int @event, string jobGroup, string jobName)
    {
        ArgumentNullException.ThrowIfNull(_cache);

        await _semaphoreSlim.WaitAsync();
        try
        {
            var data = _cache
        .Where(m =>
            m.EventId == @event &&
            m.JobGroup != null && m.JobGroup.Equals(jobGroup, StringComparison.CurrentCultureIgnoreCase) &&
            m.JobName != null && m.JobName.Equals(jobName, StringComparison.CurrentCultureIgnoreCase));

            return data;
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }
}