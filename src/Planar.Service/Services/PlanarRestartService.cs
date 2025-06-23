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

internal sealed class PlanarRestartService(IServiceProvider serviceProvider, IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    private const int _bytesInMegaByte = 1024 * 1024;
    private int _memoryHighCount;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IScheduler _scheduler = serviceProvider.GetRequiredService<IScheduler>();
    private readonly ILogger<PlanarRestartService> _logger = serviceProvider.GetRequiredService<ILogger<PlanarRestartService>>();
    private readonly SchedulerHealthCheckUtil _schedulerHealthCheckUtil = serviceProvider.GetRequiredService<SchedulerHealthCheckUtil>();
    private DateTimeOffset? _lastMemoryLog;
    private DateTime? _nextRegularRestart;
    private bool _invokeRegularRestart;

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Check every minute
            await SafeCheckForRestart();
        }

        Interlocked.Exchange(ref _memoryHighCount, 0);
    }

    private async Task SafeCheckForRestart()
    {
        try
        {
            // 1. Obtain the current application process
            var currentProcess = Process.GetCurrentProcess();

            // 2. Obtain the used memory by the process
            var usedMemory = currentProcess.PrivateMemorySize64 / _bytesInMegaByte;

            if (IsMemoryHigh(usedMemory))
            {
                SafeLog(usedMemory);
                if (!AppSettings.Protection.RestartOnHighMemoryUsage) { return; }
                await GracefulShutDown();
            }
            else if (!await IsSchedulerHealthy())
            {
                _logger.LogCritical("scheduler engine is unhealthy. start graceful end of the process. stand by scheduler");
                await GracefulShutDown();
            }
            else
            {
                await CheckForRegularRestart();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail monitor service memory at timer elapsed");
        }
    }

    private async Task CheckForRegularRestart()
    {
        if (_invokeRegularRestart)
        {
            var current = (await _scheduler.GetCurrentlyExecutingJobs()).Count;
            if (current == 0)
            {
                await GracefulRestart();
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
            return await _schedulerHealthCheckUtil.IsHealthyAsync();
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

    private void SafeMonitorRegularApplicationRestart()
    {
        try
        {
            var info = new MonitorSystemInfo("Regular restart invoked. Original restart date {{OriginDate}}");
            info.MessagesParameters.Add("OriginDate", _nextRegularRestart.ToString());
            info.AddMachineName();
            MonitorUtil.SafeSystemScan(serviceScopeFactory, _logger, MonitorEvents.RegularApplicationRestart, info, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail to invoke monitor of regular application restart");
        }
    }

    private async Task GracefulShutDown()
    {
        await SafeStandBy();
        await SafeShutdown();
        await SafeDelay();
        CloseApplication();
    }

    private async Task GracefulRestart()
    {
        SafeRestartLog();
        SafeMonitorRegularApplicationRestart();
        await SafeStandBy();
        await SafeShutdown(withLog: false);
        await SafeDelay(withLog: false);
        CloseApplication(withLog: false);
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

    private void CloseApplication(bool withLog = true)
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
            });

            var success = task.Wait(TimeSpan.FromMinutes(5));
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

    private async Task SafeDelay(bool withLog = true)
    {
        try
        {
            // extra time to handle monitors
            if (withLog)
            {
                _logger.LogCritical("memory usage is too high. wait extra 30 seconds to handle monitoring");
            }
            await Task.Delay(30_000);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "fail to delay shutdown (regular restart or high memory usage)");
        }
    }

    private async Task SafeShutdown(bool withLog = true)
    {
        try
        {
            var count = (await _scheduler.GetCurrentlyExecutingJobs()).Count;
            if (withLog)
            {
                _logger.LogCritical("memory usage is too high. shut down scheduler (with wait until complete {Count} current tasks)", count);
            }
        }
        catch (Exception ex)
        {
            if (withLog)
            {
                _logger.LogCritical(ex, "memory usage is too high. shut down scheduler (with wait until complete current tasks)");
            }
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(20));
            await _scheduler.Shutdown(true, cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "fail to shutdown scheduler (regular restart / high memory usage / scheduler unhealthy)");
        }
    }

    private async Task SafeStandBy()
    {
        try
        {
            await _scheduler.Standby();
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

    private void SafeRestartLog()
    {
        try
        {
            _logger.LogWarning("regular restart invoked. original restart date {Date}", _nextRegularRestart);
        }
        catch
        {
            //// *** DO NOTHING *** //
        }
    }
}