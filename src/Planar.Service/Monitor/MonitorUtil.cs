using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.Data;
using Planar.Service.General;
using Planar.Service.Model;
using Quartz;
using Quartz.Util;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Monitor;

public class MonitorUtil(IServiceScopeFactory serviceScopeFactory, MonitorScanProducer producer, ILogger<MonitorUtil> logger) : IMonitorUtil
{
    private readonly ILogger<MonitorUtil> _logger = logger;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

    public void Scan(MonitorEvents @event, IJobExecutionContext context, Exception? exception = default, CancellationToken cancellationToken = default)
    {
        var message = new MonitorScanMessage(@event, context, exception);
        producer.Publish(message, cancellationToken);
    }

    internal void Scan(MonitorEvents @event, MonitorSystemInfo info, Exception? exception = default, CancellationToken cancellationToken = default)
    {
        var message = new MonitorScanMessage(@event, info, exception);
        producer.Publish(message, cancellationToken);

        message = new MonitorScanMessage(MonitorEvents.AnySystemEvent, info, exception);
        producer.Publish(message, cancellationToken);
    }

    internal static void Lock<T>(Key<T> key, int lockSeconds, params MonitorEvents[] events)
    {
        foreach (var item in events)
        {
            LockJobOrTriggerEvent(key, lockSeconds, item);
        }
    }

    internal static void SafeSystemScan(IServiceProvider serviceProvider, ILogger logger, MonitorEvents @event, MonitorSystemInfo details, Exception? exception = default, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!MonitorEventsExtensions.IsSystemMonitorEvent(@event)) { return; }

            using var scope = serviceProvider.CreateScope();
            var monitorUtil = scope.ServiceProvider.GetRequiredService<MonitorUtil>();
            monitorUtil.Scan(@event, details, exception, cancellationToken);
        }
        catch (ObjectDisposedException)
        {
            ServiceUtil.AddDisposeWarningToLog(logger);
        }
        catch (Exception ex)
        {
            var source = nameof(SafeSystemScan);
            logger.LogCritical(ex, "Error handle {Source}: {Message} ", source, ex.Message);
        }
    }

    internal static void SafeSystemScan(IServiceScopeFactory serviceScopeFactory, ILogger logger, MonitorEvents @event, MonitorSystemInfo details, Exception? exception = default)
    {
        try
        {
            if (!MonitorEventsExtensions.IsSystemMonitorEvent(@event)) { return; }

            using var scope = serviceScopeFactory.CreateScope();
            var monitorUtil = scope.ServiceProvider.GetRequiredService<MonitorUtil>();
            monitorUtil.Scan(@event, details, exception);
        }
        catch (ObjectDisposedException)
        {
            ServiceUtil.AddDisposeWarningToLog(logger);
        }
        catch (Exception ex)
        {
            var source = nameof(SafeSystemScan);
            logger.LogCritical(ex, "Error handle {Source}: {Message} ", source, ex.Message);
        }
    }

    internal async Task ExecuteMonitor(MonitorAction action, MonitorEvents @event, IJobExecutionContext context, Exception? exception, CancellationToken cancellationToken = default)
    {
        var message = new MonitorScanMessage(action, @event, context, exception);
        await producer.PublishAsync(message, cancellationToken);
    }

    internal async Task ExecuteMonitor(MonitorAction action, MonitorEvents @event, MonitorSystemInfo info, Exception? exception, CancellationToken cancellationToken = default)
    {
        var message = new MonitorScanMessage(action, @event, info, exception);
        await producer.PublishAsync(message, cancellationToken);
    }

    internal async Task Validate()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dal = scope.ServiceProvider.GetRequiredService<MonitorData>();
        var count = await dal.GetMonitorCount();
        if (count == 0)
        {
            _logger.LogWarning("there is no monitor items. service does not have any monitor");
        }

        var hooks = await dal.GetMonitorUsedHooks();
        var missingHooks = hooks.Where(h => !ServiceUtil.MonitorHooks.ContainsKey(h)).ToList();
        missingHooks.ForEach(h => _logger.LogWarning("monitor item with hook {Hook} is invalid. missing hook", h));
    }

    internal static string GetMonitorEventTitle(MonitorAction monitorAction)
    {
        var title = ((MonitorEvents)monitorAction.EventId).GetEnumDescription();
        if (string.IsNullOrWhiteSpace(monitorAction.EventArgument))
        {
            return title;
        }

        return GetMonitorEventTitle(title, monitorAction.EventArgument);
    }

    internal static string GetMonitorEventTitle(int eventId, string? arguments)
    {
        var title = ((MonitorEvents)eventId).GetEnumDescription();
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return title;
        }

        return GetMonitorEventTitle(title, arguments);
    }

    internal static string GetMonitorEventTitle(string title, string? eventArguments)
    {
        if (string.IsNullOrWhiteSpace(eventArguments)) { return title; }

        var args = eventArguments.Split(',');
        var argChar = 'x';
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            var pharse = $"{{{argChar}}}";
            if (title.Contains(pharse))
            {
                title = title.Replace(pharse, arg);
            }

            argChar++;
        }

        return title;
    }

    private static void LockJobOrTriggerEvent<T>(Key<T> key, int lockSeconds, MonitorEvents @event)
    {
        var keyString = $"{key} {@event}";
        MonitorScanProducer.LockJobOrTriggerEvent(keyString, lockSeconds);
    }
}