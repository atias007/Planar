using Planar.CLI.Actions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace Planar.CLI.General;

internal static class JobTriggerIdResolver
{
    private static readonly HashSet<string> _jobIds = [];
    private static readonly HashSet<string> _triggerIds = [];
    private static readonly Timer _timer = new(TimeSpan.FromMinutes(10));
    private static readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    public static async Task Initialize()
    {
        try
        {
            await TimerElapsed();
        }
        catch
        {
            // *** DO NOTHING *** //
        }

        _timer.Elapsed += async (s, e) => await TimerElapsed();
        _timer.Start();
    }

    public static async Task<bool> SafeIsTriggerId(string triggerId)
    {
        try
        {
            return await IsTriggerIdInner(triggerId);
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> SafeIsJobId(string jobId)
    {
        try
        {
            return await IsJobIdInner(jobId);
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> IsJobIdInner(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId)) { return false; }
        await _semaphoreSlim.WaitAsync();
        try
        {
            if (_jobIds.Contains(jobId)) { return true; }
        }
        finally
        {
            _semaphoreSlim.Release();
        }

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
            return true;
        }

        return false;
    }

    private static async Task<bool> IsTriggerIdInner(string triggerId)
    {
        if (string.IsNullOrWhiteSpace(triggerId)) { return false; }
        await _semaphoreSlim.WaitAsync();
        try
        {
            if (_triggerIds.Contains(triggerId)) { return true; }
        }
        finally
        {
            _semaphoreSlim.Release();
        }

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
            return true;
        }

        return false;
    }

    private static async Task TimerElapsed()
    {
        await SafeRefreshJobIds();
        await SafeRefreshTriggerIds();
    }

    private static async Task SafeRefreshJobIds()
    {
        try
        {
            var jobIds = await SpecialActions.GetJobIds();
            if (jobIds == null) { return; }
            await _semaphoreSlim.WaitAsync();
            try
            {
                _jobIds.Clear();
                foreach (var jobId in jobIds)
                {
                    _jobIds.Add(jobId);
                }
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
    }

    private static async Task SafeRefreshTriggerIds()
    {
        try
        {
            var triggerIds = await SpecialActions.GetTriggerIds();
            if (triggerIds == null) { return; }
            await _semaphoreSlim.WaitAsync();
            try
            {
                _triggerIds.Clear();
                foreach (var triggerId in triggerIds)
                {
                    _triggerIds.Add(triggerId);
                }
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
    }
}