using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IJobDetail = Quartz.IJobDetail;
using Timer = System.Timers.Timer;

namespace Planar.Service.General;

internal class JobDetailsResolver
{
    private readonly HashSet<IJobDetail> _cache = [];
    private readonly SemaphoreSlim _locker = new(1, 1);
    private readonly ILogger<JobDetailsResolver> _logger;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly TimeSpan _timeout = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    private bool _initialized;

    public JobDetailsResolver(ILogger<JobDetailsResolver> logger, ISchedulerFactory schedulerFactory)
    {
        _logger = logger;
        _schedulerFactory = schedulerFactory;
        var timer = new Timer(_interval);
        timer.Elapsed += async (sender, args) => await FillCache().ConfigureAwait(false);
        timer.Start();
    }

    public async Task InitializeAsync()
    {
        var fill = await FillCache().ConfigureAwait(false);
        if (!fill)
        {
            throw new InvalidOperationException($"Fail to initialize {nameof(JobDetailsResolver)} resolver");
        }
    }

    public async Task<IEnumerable<IJobDetail>> GetAllJobDetailsAsync(string? group)
    {
        await LazyInitialize();
        await _locker.WaitAsync(_timeout).ConfigureAwait(false);
        try
        {
            return
                string.IsNullOrWhiteSpace(group) ?
                _cache :
                _cache.Where(c => string.Equals(group, c.Key.Group, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _locker.Release();
        }
    }

    public async Task<IEnumerable<IJobDetail>> GetSystemJobDetailsAsync()
    {
        return await GetAllJobDetailsAsync(Consts.PlanarSystemGroup);
    }

    public async Task<IEnumerable<IJobDetail>> GetUserJobDetailsAsync(string? group)
    {
        await LazyInitialize();
        await _locker.WaitAsync(_timeout).ConfigureAwait(false);
        try
        {
            var baseCache = _cache.Where(c => !string.Equals(Consts.PlanarSystemGroup, c.Key.Group, StringComparison.OrdinalIgnoreCase));
            return
                string.IsNullOrWhiteSpace(group) ?
                baseCache :
                baseCache.Where(c => string.Equals(group, c.Key.Group, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _locker.Release();
        }
    }

    private async Task LazyInitialize()
    {
        if (!_initialized)
        {
            await InitializeAsync().ConfigureAwait(false);
        }
    }

    public async Task<bool> FillCache()
    {
        HashSet<IJobDetail> data;
        try
        {
            data = await LoadDataAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fail to {Action} as {Name} resolver", nameof(LoadDataAsync), nameof(JobDetailsResolver));
            return false;
        }

        await _locker.WaitAsync(_timeout).ConfigureAwait(false);

        try
        {
            _cache.Clear();
            foreach (var item in data)
            {
                _cache.Add(item);
            }

            _initialized = true;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fail to {Action} as {Name} resolver", nameof(LoadDataAsync), nameof(JobDetailsResolver));
            return false;
        }
        finally
        {
            _locker.Release();
        }
    }

    private async Task<HashSet<IJobDetail>> LoadDataAsync()
    {
        var result = new HashSet<IJobDetail>();
        var scheduler = await _schedulerFactory.GetScheduler();
        var jobs = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
        var tasks = jobs.Select(async j => await scheduler.GetJobDetail(j));
        await Task.WhenAll(tasks);
        foreach (var task in tasks)
        {
            if (task.Result == null) { continue; }
            result.Add(task.Result);
        }

        return result;
    }
}