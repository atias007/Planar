using Microsoft.Extensions.Logging;
using Planar.Hook;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Planar.Hooks;

public sealed class PlanarLogHook(ILogger<PlanarLogHook> logger) : BaseSystemHook(logger)
{
    public override string Name => "Planar.Log";

    public override string Description =>
"""
This hook does nothing but add an entry to Planar logger.
You can find the configuration of logger in appsettings.yml (Data folder of Planar).
""";

    public override Task Handle(IMonitorDetails monitorDetails)
    {
        var scopeData = new Dictionary<string, object>
        {
            ["Application"] = "Planar",
            ["EventTitle"] = monitorDetails.EventTitle,
            ["EventId"] = monitorDetails.EventId,
            ["Environment"] = monitorDetails.Environment,
            ["FireTime"] = monitorDetails.FireTime,
            ["RunTime"] = monitorDetails.JobRunTime,
            ["FireInstanceId"] = monitorDetails.FireInstanceId,
            ["TriggerName"] = monitorDetails.TriggerName ?? "[No Trigger Name]",
            ["JobId"] = monitorDetails.JobId,
            ["TriggerId"] = monitorDetails.TriggerId,
            ["Author"] = monitorDetails.Author ?? "[No Author]",
        };

        var ex =
            string.IsNullOrWhiteSpace(monitorDetails.MostInnerExceptionMessage) ?
            null :
            new InvalidOperationException(monitorDetails.MostInnerExceptionMessage);

        var logLevel = GetLogLevel(monitorDetails);

        using var scope = logger.BeginScope(scopeData);
        logger.Log(logLevel, ex, "Planar alert: for {Job}, event: {Event}",
            $"{monitorDetails.JobGroup}.{monitorDetails.JobName}",
            monitorDetails.EventTitle);

        return Task.CompletedTask;
    }

    public override Task HandleSystem(IMonitorSystemDetails monitorDetails)
    {
        var scopeData = new Dictionary<string, object>
        {
            ["Application"] = "Planar",
            ["EventTitle"] = monitorDetails.EventTitle,
            ["EventId"] = monitorDetails.EventId,
            ["Environment"] = monitorDetails.Environment,
        };

        var ex =
            string.IsNullOrWhiteSpace(monitorDetails.MostInnerExceptionMessage) ?
            null :
            new InvalidOperationException(monitorDetails.MostInnerExceptionMessage);

        var logLevel = GetLogLevel(monitorDetails);

        using var scope = logger.BeginScope(scopeData);
        logger.Log(logLevel, ex, "Planar alert: event: {Event}, message: {Message}",
            monitorDetails.EventTitle,
            monitorDetails.Message);

        return Task.CompletedTask;
    }

    private static LogLevel GetLogLevel(IMonitor monitor)
    {
        return monitor.EventId switch
        {
            101 => LogLevel.Warning, // Execution Retry
            102 => LogLevel.Error, // Execution Last Retry Fail
            103 => LogLevel.Error, // Execution Fail
            104 => LogLevel.Information, // Execution Success
            105 => LogLevel.Information, // Execution Start
            106 => LogLevel.Information, // Execution End
            107 => LogLevel.Warning, // Execution Success With No Effected Rows
            108 => LogLevel.Information, // Execution Progress Changed
            109 => LogLevel.Warning, // Execution Timeout
            110 => LogLevel.Warning, // Execution Success With Warnings
            200 => LogLevel.Warning, // Execution Fail {x} Times In Row
            201 => LogLevel.Warning, // Execution Fail {x} Times In {y} Hours
            202 => LogLevel.Warning, // Execution End With Effected Rows Greater Than {x}
            203 => LogLevel.Warning, // Execution End With Effected Rows Less Than {x}
            204 => LogLevel.Warning, // Execution End With Effected Rows Greater Than {x} In {y} Hours
            205 => LogLevel.Warning, // Execution End With Effected Rows Less Than {x} In {y} Hours
            206 => LogLevel.Warning, // Execution Duration Greater Than {x} Minutes
            207 => LogLevel.Warning, // Execution End With More Than {x} Exceptions
            300 => LogLevel.Information, // Job Added
            301 => LogLevel.Warning, // Job Deleted
            302 => LogLevel.Warning, // Job Canceled
            303 => LogLevel.Warning, // Job Paused
            304 => LogLevel.Information, // Job Resumed
            307 => LogLevel.Error, // Scheduler Error
            308 => LogLevel.Warning, // Scheduler In Standby Mode
            309 => LogLevel.Information, // Scheduler Started
            310 => LogLevel.Warning, // Scheduler Shutdown
            311 => LogLevel.Warning, // Trigger Paused
            312 => LogLevel.Information, // Trigger Resumed
            313 => LogLevel.Information, // Cluster Node Join
            314 => LogLevel.Warning, // Cluster Node Removed
            315 => LogLevel.Error, // Cluster Health Check Fail
            316 => LogLevel.Information, // Job Updated
            317 => LogLevel.Error, // Max Memory Usage
            318 => LogLevel.Information, // Regular Application Restart
            319 => LogLevel.Warning, // Circuit Breaker Activated
            320 => LogLevel.Information, // Circuit Breaker Reset
            399 => LogLevel.Information, // Any System Event
            400 => LogLevel.Information, // Custom event 1
            401 => LogLevel.Information, // Custom event 2
            402 => LogLevel.Information, // Custom event 3
            403 => LogLevel.Information, // Custom event 4
            404 => LogLevel.Information, // Custom event 5
            _ => LogLevel.Information
        };
    }
}