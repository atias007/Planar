using Planar.CLI.Actions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace Planar.CLI.General;

internal enum IdType
{
    None,
    JobId,
    TriggerId
}

internal static class JobTriggerIdResolver
{
    private static readonly HashSet<string> _jobIds = [];
    private static readonly HashSet<string> _triggerIds = [];
    private static readonly Timer _timer = new(TimeSpan.FromMinutes(10));
    private static Timer? _initTimer;
    private static readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    public static async Task Initialize()
    {
        var success = await SafeTimerElapsed();
        if (!success)
        {
            _initTimer = new Timer(TimeSpan.FromMinutes(1));
            _initTimer.Elapsed += async (s, e) =>
            {
                if (await SafeTimerElapsed())
                {
                    _initTimer.Stop();
                    _initTimer.Dispose();
                }
            };
            _initTimer.Start();
        }

        _timer.Elapsed += async (s, e) => await SafeTimerElapsed();
        _timer.Start();
    }

    public static async Task Refresh()
    {
        await SafeTimerElapsed();
    }

    public static async Task<IdType> SafeGetIdType(string id)
    {
        try
        {
            if (await IsJobIdCached(id)) { return IdType.JobId; }
            if (await IsTriggerIdCached(id)) { return IdType.TriggerId; }
            if (await IsJobIdOnline(id)) { return IdType.JobId; }
            if (await IsTriggerIdOnline(id)) { return IdType.TriggerId; }
            return IdType.None;
        }
        catch
        {
            return IdType.None;
        }
    }

    private static async Task<bool> IsJobIdOnline(string jobId)
    {
        var isJob = await SpecialActions.IsJobId(jobId);
        if (isJob)
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                _jobIds.Add(jobId);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        return isJob;
    }

    private static async Task<bool> IsJobIdCached(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId)) { return false; }
        await _semaphoreSlim.WaitAsync();
        try
        {
            return _jobIds.Contains(jobId);
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    private static async Task<bool> IsTriggerIdOnline(string triggerId)
    {
        var isTrigger = await SpecialActions.IsTriggerId(triggerId);
        if (isTrigger)
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                _triggerIds.Add(triggerId);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        return isTrigger;
    }

    private static async Task<bool> IsTriggerIdCached(string triggerId)
    {
        if (string.IsNullOrWhiteSpace(triggerId)) { return false; }
        await _semaphoreSlim.WaitAsync();
        try
        {
            return _triggerIds.Contains(triggerId);
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    private static async Task<bool> SafeTimerElapsed()
    {
        return await SafeRefreshJobIds() && await SafeRefreshTriggerIds();
    }

    private static async Task<bool> SafeRefreshJobIds()
    {
        try
        {
            var jobIds = await SpecialActions.GetJobIds();
            if (jobIds == null) { return false; }
            await _semaphoreSlim.WaitAsync();
            try
            {
                _jobIds.Clear();
                foreach (var jobId in jobIds)
                {
                    _jobIds.Add(jobId);
                }

                return true;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
        catch
        {
            // *** DO NOTHING *** //
        }

        return false;
    }

    private static async Task<bool> SafeRefreshTriggerIds()
    {
        try
        {
            var triggerIds = await SpecialActions.GetTriggerIds();
            if (triggerIds == null) { return false; }
            await _semaphoreSlim.WaitAsync();
            try
            {
                _triggerIds.Clear();
                foreach (var triggerId in triggerIds)
                {
                    _triggerIds.Add(triggerId);
                }

                return true;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
        catch
        {
            // *** DO NOTHING *** //
        }

        return false;
    }
}