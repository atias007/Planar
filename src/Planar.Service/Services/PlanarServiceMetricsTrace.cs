using Microsoft.Extensions.DependencyInjection;
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

            return value > 5;
        }
        else
        {
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
        try
        {
            _logger.LogCritical("memory usage is too high. used memory: {UsedMemory}MB. start graceful end of the process", usedMemory);
            _logger.LogCritical("memory usage is too high. stand by scheduler");
            _timer.Stop();
            await _scheduler.Standby();
        }
        catch
        {
            //// *** DO NOTHING *** //
        }

        _logger.LogCritical("memory usage is too high. shut down scheduler (with wait until complete current tasks)");
        await _scheduler.Shutdown(true);

        // extra time to handle monitors
        _logger.LogCritical("memory usage is too high. wait extra 30 seconds to handle monitoring");
        await Task.Delay(30_000);

        try
        {
            _logger.LogCritical("memory usage is too high. stop the apllication");
            var app = _serviceProvider.GetRequiredService<IHostApplicationLifetime>();
            app.StopApplication();
        }
        catch
        {
            //// *** DO NOTHING *** //
        }

        await Task.Delay(5_000);
        Environment.Exit(-1);
    }
}