using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Common.Monitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommonJob
{
    public sealed class MonitorDurationCache
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IServiceProvider _serviceProvider;
        private const string CacheKey = "MonitorDurationCache";
        private static readonly object _locker = new();

        public MonitorDurationCache(IMemoryCache memoryCache, IServiceProvider serviceProvider)
        {
            _memoryCache = memoryCache;
            _serviceProvider = serviceProvider;
            _ = FillCacheAsync();
        }

        public IEnumerable<int> GetMonitorMinutes(Quartz.IJobExecutionContext context)
        {
            if (!_memoryCache.TryGetValue<List<MonitorCacheItem>>(CacheKey, out var data))
            {
                FillCache();
                if (!_memoryCache.TryGetValue(CacheKey, out data))
                {
                    var option = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromMinutes(20))
                        .RegisterPostEvictionCallback(EvictionCallback);

                    var empty = Array.Empty<int>();
                    _memoryCache.Set(CacheKey, empty, option);
                    return empty;
                }
            }

            if (data == null || data.Count == 0) { return Array.Empty<int>(); }

            var key = context.JobDetail.Key;
            var result = data.Where(d =>
                d.DurationLimit.GetValueOrDefault() > 0 &&
                (
                    (d.JobGroup == null && d.JobName == null) ||
                    (d.JobGroup == key.Group && d.JobName == null) ||
                    (d.JobGroup == key.Group && d.JobName == key.Name)
                ))
                .Select(d => d.DurationLimit.GetValueOrDefault())
                .ToList();

            return result;
        }

        public async Task Flush()
        {
            await FillCacheAsync();
        }

        private async Task FillCacheAsync()
        {
            await Task.Run(FillCache);
        }

        private void FillCache()
        {
            lock (_locker)
            {
                try
                {
                    _memoryCache.Remove(CacheKey);

                    var dataLayer = _serviceProvider.GetRequiredService<IMonitorDurationDataLayer>();
                    var data = dataLayer.GetDurationMonitorActions().Result;
                    var option = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromHours(1))
                        .RegisterPostEvictionCallback(EvictionCallback);

                    _memoryCache.Set(CacheKey, data, option);
                }
                catch (Exception ex)
                {
                    SafeLogCacheError(ex);
                }
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

        private void EvictionCallback(object key, object? value, EvictionReason reason, object? state)
        {
            _ = FillCacheAsync();
        }
    }
}