using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.ServiceProcess;

namespace Planar.Watcher;

internal class WatcherService(ILogger<WatcherService> logger, IConfiguration configuration, IHostApplicationLifetime lifetime) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = GetSettings();
        ValidateSettings(settings);
        LogStartInformation(settings);

        while (!stoppingToken.IsCancellationRequested)
        {
            SafeCheckServiceStatus(settings);
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private void SafeCheckServiceStatus(Settings settings)
    {
        try
        {
            CheckServiceStatus(settings);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Failed to check service status. Service may not be running or is in an invalid state.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to check service status. Service may not be running or is in an invalid state.");
        }
    }

    private void CheckServiceStatus(Settings settings)
    {
        using var controller = new ServiceController(settings.ServiceName, settings.Host);
        var status = controller.Status;
        var startType = controller.StartType;
        var disabled = status == ServiceControllerStatus.Stopped && startType == ServiceStartMode.Disabled;

        if (disabled && settings.IgnoreDisabledService)
        {
            logger.LogInformation("skipping disabled service");
            return;
        }

        if (disabled)
        {
            logger.LogError("service is disabled");
            return;
        }

        if (status == ServiceControllerStatus.Running)
        {
            logger.LogInformation("service is in running status");
            return;
        }

        var winUtil = new WindowsServiceUtil(logger, settings.ServiceName, settings.Host);

        if (status == ServiceControllerStatus.StartPending || status == ServiceControllerStatus.ContinuePending)
        {
            logger.LogWarning("service is in {Status} status. waiting for running status…", status);
            status = winUtil.WaitForStatus(controller, ServiceControllerStatus.Running, settings.StartServiceTimeout, restart: settings.KillPendingServiceProcess);
            if (status == ServiceControllerStatus.Running)
            {
                logger.LogInformation("service is in running status");
                return;
            }
        }

        if (status == ServiceControllerStatus.StopPending)
        {
            logger.LogWarning("service is in {Status} status. waiting for stopped status…", status);
            status = winUtil.WaitForStatus(controller, ServiceControllerStatus.Stopped, settings.StopPendingServiceTimeout, restart: settings.KillPendingServiceProcess);
        }

        if (status == ServiceControllerStatus.Stopped)
        {
            logger.LogWarning("service is in stopped status. starting service");
            controller.Start();
            status = winUtil.WaitForStatus(controller, ServiceControllerStatus.Running, settings.StartServiceTimeout, restart: false);
            if (status == ServiceControllerStatus.Running)
            {
                logger.LogInformation("service is in running status");
                return;
            }
        }

        if (status == ServiceControllerStatus.Paused)
        {
            logger.LogWarning("service is in paused status. continue service");
            controller.Continue();
            status = winUtil.WaitForStatus(controller, ServiceControllerStatus.Running, settings.StartServiceTimeout, restart: false);
            if (status == ServiceControllerStatus.Running)
            {
                logger.LogInformation("service is in running status");
                return;
            }
        }

        if (status != ServiceControllerStatus.Running)
        {
            throw new InvalidOperationException($"service is in {status} status");
        }
    }

    private void LogStartInformation(Settings settings)
    {
        logger.LogInformation("--------------------------------------");
        logger.LogInformation("-   Planar watcher service started   -");
        logger.LogInformation("--------------------------------------");
        logger.LogInformation("Interval: {Interval:c}", _interval);
        logger.LogInformation("Service name: {ServiceName}", settings.ServiceName);
        logger.LogInformation("Host: {Host}", settings.Host);
        logger.LogInformation("Ignore disabled service: {IngnoreDisabledService}", settings.IgnoreDisabledService);
        logger.LogInformation("StartServiceTimeout: {StartServiceTimeout:c}", settings.StartServiceTimeout);
        logger.LogInformation("Stop pending service timeout: {StopPendingServiceTimeout}", settings.StopPendingServiceTimeout);
        logger.LogInformation("Kill pending service process: {KillPendingServiceProcess}", settings.KillPendingServiceProcess);
        logger.LogInformation("--------------------------------------");
    }

    private Settings GetSettings()
    {
        var settings = configuration.Get<Settings>();
        if (settings == null)
        {
            logger.LogError("Failed to load settings from appsettings.json configuration file");
            Exit();
            ArgumentNullException.ThrowIfNull(settings);
        }

        return settings;
    }

    private void Exit()
    {
        Log.CloseAndFlush();
        lifetime.StopApplication();
    }

    private void ValidateSettings(Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ServiceName))
        {
            logger.LogError("ServiceName is not configured in appsettings.json");
            Exit();
            return;
        }
        if (string.IsNullOrWhiteSpace(settings.Host))
        {
            logger.LogError("Host is not configured in appsettings.json");
            Exit();
            return;
        }

        if (settings.StartServiceTimeout <= TimeSpan.Zero)
        {
            logger.LogError("StartServiceTimeout must be greater than zero");
            Exit();
            return;
        }

        if (settings.StopPendingServiceTimeout <= TimeSpan.Zero)
        {
            logger.LogError("StopPendingServiceTimeout must be greater than zero");
            Exit();
        }
    }
}