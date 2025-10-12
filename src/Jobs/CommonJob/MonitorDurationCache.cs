using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Common.Monitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CommonJob;

public sealed class MonitorDurationCache
{
    private readonly IMemoryCache _memoryCache;
    private readonly IServiceProvider _serviceProvider;
    private const string CacheKey = "MonitorDurationCache";
    private static readonly SemaphoreSlim _locker = new(1, 1);

    public MonitorDurationCache(IMemoryCache memoryCache, IServiceProvider serviceProvider)
    {
        _memoryCache = memoryCache;
        _serviceProvider = serviceProvider;
        _ = FillCache();
    }

    public async Task<List<int>> GetMonitorMinutes(Quartz.IJobExecutionContext context)
    {
        if (!_memoryCache.TryGetValue<List<MonitorCacheItem>>(CacheKey, out var data))
        {
            await FillCache();
            if (!_memoryCache.TryGetValue(CacheKey, out data))
            {
                var empty = Array.Empty<int>().ToList();
                _memoryCache.Set(CacheKey, empty, TimeSpan.FromMinutes(20));
                return empty;
            }
        }

        if (data == null || data.Count == 0) { return []; }

        var key = context.JobDetail.Key;
        var result = data.Where(d =>
            d.DurationLimit.GetValueOrDefault() > 0 &&
            (
                (d.JobGroup == null && d.JobName == null) ||
                (d.JobGroup == key.Group && d.JobName == null) ||
                (d.JobGroup == key.Group && d.JobName == key.Name)
            ))
            .Select(d => d.DurationLimit.GetValueOrDefault())
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        return result;
    }

    public async Task Flush()
    {
        await FillCache();
    }

    private async Task FillCache()
    {
        await _locker.WaitAsync();
        try
        {
            _memoryCache.Remove(CacheKey);

            var dataLayer = _serviceProvider.GetRequiredService<IMonitorDurationDataLayer>();
            var data = await dataLayer.GetDurationMonitorActions();
            _memoryCache.Set(CacheKey, data, TimeSpan.FromHours(1));
        }
        catch (Exception ex)
        {
            SafeLogCacheError(ex);
        }
        finally
        {
            _locker.Release();
        }
    }

    private void SafeLogCacheError(Exception ex)
    {
        try
        {
            var logger = _serviceProvider.GetRequiredService<ILogger<MonitorDurationCache>>();
            logger.LogCritical(ex, "Fail to fill cache for duration monitor");
        }
        catch
        {
            // *** DO NOTHING *** //
        }
    }
}