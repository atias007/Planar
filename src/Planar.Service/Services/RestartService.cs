using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.General;
using Planar.Service.Monitor;
using Quartz;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Services;

internal sealed class RestartService(IServiceProvider serviceProvider, IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    private const int _bytesInMegaByte = 1024 * 1024;
    private int _memoryHighCount;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IScheduler _scheduler = serviceProvider.GetRequiredService<IScheduler>();
    private readonly ILogger<RestartService> _logger = serviceProvider.GetRequiredService<ILogger<RestartService>>();
    private readonly SchedulerUtil _schedulerUtil = serviceProvider.GetRequiredService<SchedulerUtil>();
    private DateTimeOffset? _lastMemoryLog;
    private readonly DateTimeOffset _startup = DateTimeOffset.UtcNow;
    private DateTime? _nextRegularRestart;
    private bool _invokeRegularRestart;

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Wait(stoppingToken);
            await SafeCheckForRestart(stoppingToken);
        }

        Interlocked.Exchange(ref _memoryHighCount, 0);
    }

    private static async Task Wait(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Check every minute
        }
        catch (TaskCanceledException)
        {
            // *** DO NOHING *** //
        }
    }

    private async Task SafeCheckForRestart(CancellationToken stoppingToken)
    {
        if (_startup.Subtract(DateTimeOffset.UtcNow).TotalMinutes < 5)
        {
            // Skip the first 5 minute after startup to avoid false positives
            return;
        }

        try
        {
            // 1. Obtain the current application process
            var currentProcess = Process.GetCurrentProcess();

            // 2. Obtain the used memory by the process
            var usedMemory = currentProcess.PrivateMemorySize64 / _bytesInMegaByte;

            if (IsMemoryHigh(usedMemory))
            {
                if (stoppingToken.IsCancellationRequested) { return; } // Exit if cancellation is requested
                SafeLog(usedMemory);
                if (!AppSettings.Protection.RestartOnHighMemoryUsage) { return; }
                await GracefulShutDown(stoppingToken);
            }
            else if (!await IsSchedulerHealthy())
            {
                if (stoppingToken.IsCancellationRequested) { return; } // Exit if cancellation is requested
                _logger.LogCritical("scheduler engine is unhealthy. start graceful end of the process. stand by scheduler");
                await GracefulShutDown(stoppingToken);
            }
            else
            {
                await CheckForRegularRestart(stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail monitor service memory at timer elapsed");
        }
    }

    private async Task CheckForRegularRestart(CancellationToken cancellationToken)
    {
        if (_invokeRegularRestart)
        {
            var current = (await _scheduler.GetCurrentlyExecutingJobs(cancellationToken)).Count;
            if (current == 0)
            {
                await GracefulRestart(cancellationToken);
            }

            return;
        }

        if (!AppSettings.Protection.HasRegularRestart) { return; }
        if (_nextRegularRestart == null)
        {
            _nextRegularRestart = GetNextRegularRestartDate();
        }

        if (_nextRegularRestart == null)
        {
            AppSettings.Protection.RegularRestartExpression = null;
            return;
        }

        if (DateTimeOffset.UtcNow >= _nextRegularRestart)
        {
            _invokeRegularRestart = true;
        }
    }

    private async Task<bool> IsSchedulerHealthy()
    {
        try
        {
            return await _schedulerUtil.IsHealthyAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail to check scheduler health status");
            return true; // If we can't determine health, assume healthy
        }
    }

    private bool IsMemoryHigh(long usedMemory)
    {
        if (usedMemory > AppSettings.Protection.MaxMemoryUsage)
        {
            var value = Interlocked.Increment(ref _memoryHighCount);
            if (value == 1)
            {
                SafeMonitorMaxMemoryUsage(usedMemory);
            }

            return value > AppSettings.Protection.WaitBeforeRestartMinutes;
        }
        else
        {
            // reset counter
            Interlocked.Exchange(ref _memoryHighCount, 0);
            return false;
        }
    }

    private void SafeMonitorMaxMemoryUsage(long usedMemory)
    {
        try
        {
            var info = new MonitorSystemInfo("Memory usage at {{MachineName}} is too high ({{UsedMemoryMB}}MB)");
            info.MessagesParameters.Add("UsedMemoryMB", usedMemory.ToString(CultureInfo.CurrentCulture));
            info.AddMachineName();
            MonitorUtil.SafeSystemScan(serviceScopeFactory, _logger, MonitorEvents.MaxMemoryUsage, info, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail to invoke monitor of memory usage to high");
        }
    }

    private void SafeMonitorRegularApplicationRestart(CancellationToken cancellationToken)
    {
        try
        {
            var info = new MonitorSystemInfo("Regular restart invoked. Original restart date {{OriginDate}}");
            info.MessagesParameters.Add("OriginDate", _nextRegularRestart.ToString());
            info.AddMachineName();
            if (cancellationToken.IsCancellationRequested) { return; } // Exit if cancellation is requested
            MonitorUtil.SafeSystemScan(serviceScopeFactory, _logger, MonitorEvents.RegularApplicationRestart, info, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail to invoke monitor of regular application restart");
        }
    }

    private async Task GracefulShutDown(CancellationToken cancellationToken)
    {
        await SafeStandBy(cancellationToken);
        await SafeShutdown(withLog: true, cancellationToken);
        await SafeDelay(withLog: true, cancellationToken);
        CloseApplication(withLog: true, cancellationToken);
    }

    private async Task GracefulRestart(CancellationToken cancellationToken)
    {
        SafeRestartLog(cancellationToken);
        SafeMonitorRegularApplicationRestart(cancellationToken);
        await SafeStandBy(cancellationToken);
        await SafeShutdown(withLog: false, cancellationToken);
        await SafeDelay(withLog: false, cancellationToken);
        CloseApplication(withLog: false, cancellationToken);
    }

    private DateTime? GetNextRegularRestartDate()
    {
        try
        {
            if (!AppSettings.Protection.HasRegularRestart) { return null; }
            var exp = new CronExpression(AppSettings.Protection.RegularRestartExpression ?? string.Empty);
            var result = exp.GetNextValidTimeAfter(DateTimeOffset.Now);
            return result?.LocalDateTime;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "fail to get next regular restart date from settings. cron expression is: {Expression}", AppSettings.Protection.RegularRestartExpression);
            return null;
        }
    }

    private void CloseApplication(bool withLog, CancellationToken cancellationToken)
    {
        try
        {
            var task = Task.Run(() =>
            {
                if (withLog)
                {
                    _logger.LogCritical("stop the apllication due to system failure");
                }

                var app = _serviceProvider.GetRequiredService<IHostApplicationLifetime>();
                app.StopApplication();
            }, cancellationToken);

            var success = task.Wait(TimeSpan.FromMinutes(5), cancellationToken);
            if (!success)
            {
                Environment.Exit(-1);
            }
        }
        catch
        {
            //// *** DO NOTHING *** //
        }
    }

    private async Task SafeDelay(bool withLog, CancellationToken cancellationToken)
    {
        try
        {
            // extra time to handle monitors
            if (withLog)
            {
                if (cancellationToken.IsCancellationRequested) { return; } // Exit if cancellation is requested
                _logger.LogCritical("memory usage is too high. wait extra 30 seconds to handle monitoring");
            }
            await Task.Delay(30_000, cancellationToken);
        }
        catch (Exception ex)
        {
            if (cancellationToken.IsCancellationRequested) { return; } // Exit if cancellation is requested
            _logger.LogCritical(ex, "fail to delay shutdown (regular restart or high memory usage)");
        }
    }

    private async Task SafeShutdown(bool withLog, CancellationToken cancellationToken)
    {
        try
        {
            var count = (await _scheduler.GetCurrentlyExecutingJobs(cancellationToken)).Count;
            if (count > 0 && withLog)
            {
                _logger.LogCritical("shut down scheduler (with wait until complete {Count} current tasks)", count);
            }
        }
        catch (Exception ex)
        {
            if (withLog)
            {
                _logger.LogCritical(ex, "fail to shut down scheduler");
            }
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(20));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
            await _scheduler.Shutdown(true, linked.Token);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "fail to shutdown scheduler (regular restart / high memory usage / scheduler unhealthy)");
        }
    }

    private async Task SafeStandBy(CancellationToken cancellationToken)
    {
        try
        {
            await _scheduler.Standby(cancellationToken);
        }
        catch
        {
            //// *** DO NOTHING *** //
        }
    }

    private void SafeLog(long usedMemory)
    {
        try
        {
            if (_lastMemoryLog == null || DateTimeOffset.Now.Subtract(_lastMemoryLog.GetValueOrDefault()).TotalHours > 1)
            {
                _logger.LogCritical("memory usage is too high. used memory: {UsedMemory}MB. start graceful end of the process. stand by scheduler", usedMemory);
                _lastMemoryLog = DateTimeOffset.Now;
            }
        }
        catch
        {
            //// *** DO NOTHING *** //
        }
    }

    private void SafeRestartLog(CancellationToken cancellationToken)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested) { return; } // Exit if cancellation is requested
            _logger.LogWarning("regular restart invoked. original restart date {Date}", _nextRegularRestart);
        }
        catch
        {
            //// *** DO NOTHING *** //
        }
    }
}