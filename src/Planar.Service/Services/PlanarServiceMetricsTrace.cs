﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.Monitor;
using Quartz;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace Planar.Service.Services;

internal sealed class PlanarServiceMetricsTrace(IServiceProvider serviceProvider, IServiceScopeFactory serviceScopeFactory) : IHostedService, IDisposable
{
    private const int _bytesInMegaByte = 1024 * 1024;
    private int _memoryHighCount;
    private readonly Timer _timer = new(TimeSpan.FromMinutes(1));
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IScheduler _scheduler = serviceProvider.GetRequiredService<IScheduler>();
    private readonly ILogger<PlanarServiceMetricsTrace> _logger = serviceProvider.GetRequiredService<ILogger<PlanarServiceMetricsTrace>>();
    private DateTimeOffset? _lastMemoryLog;

    public void Dispose()
    {
        _timer.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer.Elapsed += TimerElapsed;
        _timer.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Stop();
        Interlocked.Exchange(ref _memoryHighCount, 0);
        return Task.CompletedTask;
    }

    private void TimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        try
        {
            // 1. Obtain the current application process
            Process currentProcess = Process.GetCurrentProcess();

            // 2. Obtain the used memory by the process
            var usedMemory = currentProcess.PrivateMemorySize64 / _bytesInMegaByte;

            if (IsMemoryHigh(usedMemory))
            {
                _ = GracefullShutDown(usedMemory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail monitor service memory at timer elapsed");
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

    private async Task GracefullShutDown(long usedMemory)
    {
        SafeLog(usedMemory);
        if (!AppSettings.Protection.RestartOnHighMemoryUsage) { return; }
        await SafeStandBy();
        await SafeShutdown();
        await SafeDelay();
        CloseApplication();
    }

    private void CloseApplication()
    {
        try
        {
            var task = Task.Run(() =>
            {
                _logger.LogCritical("memory usage is too high. stop the apllication");
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

    private async Task SafeDelay()
    {
        try
        {
            // extra time to handle monitors
            _logger.LogCritical("memory usage is too high. wait extra 30 seconds to handle monitoring");
            await Task.Delay(30_000);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "fail to delay shutdown on high memory usage trace service");
        }
    }

    private async Task SafeShutdown()
    {
        try
        {
            var count = (await _scheduler.GetCurrentlyExecutingJobs()).Count;
            _logger.LogCritical("memory usage is too high. shut down scheduler (with wait until complete {Count} current tasks)", count);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "memory usage is too high. shut down scheduler (with wait until complete current tasks)");
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(20));
            await _scheduler.Shutdown(true, cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "fail to shutdown scheduler on high memory usage trace service");
        }
    }

    private async Task SafeStandBy()
    {
        try
        {
            _timer.Stop();
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
                _logger.LogCritical("memory usage is too high. used memory: {UsedMemory}MB. start graceful end of the process", usedMemory);
                _logger.LogCritical("memory usage is too high. stand by scheduler");
                _lastMemoryLog = DateTimeOffset.Now;
            }
        }
        catch
        {
            //// *** DO NOTHING *** //
        }
    }
}