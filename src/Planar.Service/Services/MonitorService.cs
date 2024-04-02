using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Planar.Common;
using Planar.Common.Exceptions;
using Planar.Common.Helpers;
using Planar.Service.API;
using Planar.Service.API.Helpers;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Planar.Service.Model;
using Planar.Service.Monitor;
using Planar.Service.Validation;
using Quartz;
using Quartz.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Planar.Service.Services;

internal class MonitorService(IServiceProvider serviceProvider, IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    private readonly Channel<MonitorScanMessage> _channel = serviceProvider.GetRequiredService<Channel<MonitorScanMessage>>();
    private readonly ILogger<MonitorService> _logger = serviceProvider.GetRequiredService<ILogger<MonitorService>>();
    private const string nullText = "[null]";
    private static int _instanceCount;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reader = _channel.Reader;
        while (!reader.Completion.IsCompleted && await reader.WaitToReadAsync(stoppingToken))
        {
            if (!reader.TryRead(out var monitor)) { continue; }
            if (IsInternalEvent(monitor)) { continue; }

            switch (monitor.Type)
            {
                case MonitorScanType.ScanJob:
                    _ = SafeScanInner(monitor.Event, monitor.JobExecutionContext, monitor.Exception, stoppingToken);
                    break;

                case MonitorScanType.ScanSystem:
                    _ = SafeScanInner(monitor.Event, monitor.MonitorSystemInfo, monitor.Exception, stoppingToken);
                    break;

                case MonitorScanType.ExecuteJob:
                    _ = SafeExecuteMonitor(monitor.MonitorAction, monitor.Event, monitor.JobExecutionContext, monitor.Exception, stoppingToken);
                    break;

                case MonitorScanType.ExecuteSystem:
                    _ = SafeExecuteMonitor(monitor.MonitorAction, monitor.Event, monitor.MonitorSystemInfo, monitor.Exception, stoppingToken);
                    break;

                default:
                    break;
            }
        }

        _channel.Writer.TryComplete();
    }

    private static bool IsInternalEvent(MonitorScanMessage message)
    {
        if (message.JobExecutionContext != null && JobKeyHelper.IsSystemJobKey(message.JobExecutionContext.JobDetail.Key)) { return true; }
        return false;
    }

    private async Task SafeScanInner(MonitorEvents @event, MonitorSystemInfo? info, Exception? exception, CancellationToken cancellationToken)
    {
        try
        {
            await ScanInner(@event, info, exception, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail to handle monitor item(s)");
        }
    }

    private async Task SafeScanInner(MonitorEvents @event, IJobExecutionContext? context, Exception? exception, CancellationToken cancellationToken)
    {
        try
        {
            await ScanInner(@event, context, exception, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail to handle monitor item(s)");
        }
    }

    private async Task ScanInner(MonitorEvents @event, MonitorSystemInfo? info, Exception? exception, CancellationToken cancellationToken)
    {
        if (info == null)
        {
            _logger.LogWarning("MonitorSystemInfo is null in {MethodName}. Scan skipped", $"{nameof(MonitorService)}.{nameof(ScanInner)}");
            return;
        }

        IEnumerable<MonitorAction> items;

        try
        {
            items = await LoadMonitorItems(@event);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail to handle monitor item(s) --> LoadMonitorItems");
            return;
        }

        foreach (var action in items)
        {
            _ = SafeExecuteMonitor(action, @event, info, exception, cancellationToken);
        }
    }

    private async Task ScanInner(MonitorEvents @event, IJobExecutionContext? context, Exception? exception = default, CancellationToken cancellationToken = default)
    {
        if (context == null)
        {
            _logger.LogWarning("IJobExecutionContext is null in {MethodName}. Scan skipped", $"{nameof(MonitorService)}.{nameof(ScanInner)}");
            return;
        }

        if (context.JobDetail.Key.Group.StartsWith(Consts.PlanarSystemGroup))
        {
            return;
        }

        IEnumerable<MonitorAction> items;

        try
        {
            items = await LoadMonitorItems(@event, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail to handle monitor item(s) --> LoadMonitorItems");
            return;
        }

        foreach (var action in items)
        {
            _ = SafeExecuteMonitor(action, @event, context, exception, cancellationToken);
        }
    }

    #region Load Monitor Items

    private async Task<IEnumerable<MonitorAction>> LoadMonitorItems(MonitorEvents @event, IJobExecutionContext context)
    {
        // Check Cache
        if (!MonitorServiceCache.IsCacheValid)
        {
            var items = await LoadAllMonitorItems();
            MonitorServiceCache.SetCache(items);
        }

        var result = MonitorServiceCache.GetMonitorActions(@event, context);
        return result;
    }

    private async Task<IEnumerable<MonitorAction>> LoadMonitorItems(MonitorEvents @event)
    {
        // Check Cache
        if (!MonitorServiceCache.IsCacheValid)
        {
            var items = await LoadAllMonitorItems();
            MonitorServiceCache.SetCache(items);
        }

        var result = MonitorServiceCache.GetMonitorActions(@event);
        return result;
    }

    private async Task<IEnumerable<MonitorAction>> LoadAllMonitorItems()
    {
        using var scope = serviceScopeFactory.CreateScope();
        var bl = scope.ServiceProvider.GetRequiredService<MonitorDomain>();
        var data = await bl.GetMonitorActions();
        return data;
    }

    #endregion Load Monitor Items

    #region Execute Monitor

    private async Task SafeExecuteMonitor(MonitorAction? action, MonitorEvents @event, IJobExecutionContext? context, Exception? exception, CancellationToken cancellationToken)
    {
        MonitorDetails? details = null;
        if (action == null) { return; }
        if (context == null) { return; }

        try
        {
            // Analyze
            var toBeContinue = await Analyze(@event, action, context);
            if (!toBeContinue) { return; }

            // Get hook
            var hookInstance = ServiceUtil.MonitorHooks.TryGetAndReturn(action.Hook);
            if (hookInstance == null)
            {
                _logger.LogWarning("hook {Hook} in monitor item id: {Id}, title: {Title} does not exist in service", action.Hook, action.Id, action.Title);
                return;
            }
            else
            {
                // Create the monitor details
                details = GetMonitorDetails(action, context, exception);

                // Check for mute
                if (await CheckForMutedMonitor(details, action.Id))
                {
                    _logger.LogWarning("monitor item id: {Id}, title: {Title} is muted", action.Id, action.Title);
                    return;
                }

                // Log the start of the monitor
                if (@event == MonitorEvents.ExecutionProgressChanged)
                {
                    _logger.LogDebug("monitor item id: {Id}, title: {Title} start to handle event {Event} with hook: {Hook} and distribution group {Group}", action.Id, action.Title, @event, action.Hook, action.Group.Name);
                }
                else
                {
                    _logger.LogInformation("monitor item id: {Id}, title: {Title} start to handle event {Event} with hook: {Hook} and distribution group {Group}", action.Id, action.Title, @event, action.Hook, action.Group.Name);
                }

                // Handle the monitor
                await hookInstance.Handle(details, cancellationToken);

                // Save the monitor alert
                await SafeSaveMonitorAlert(action, details, context);

                // Save the monitor counter
                await SaveMonitorCounter(action, details);

                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail to handle monitor item id: {Id}, title: {Title} with hook: {Hook} and distribution group {Group}", action.Id, action.Title, action.Hook, action.Group.Name);
            await SafeSaveMonitorAlert(action, details, context, ex);
        }
    }

    private async Task SafeExecuteMonitor(MonitorAction? action, MonitorEvents @event, MonitorSystemInfo? info, Exception? exception, CancellationToken cancellationToken)
    {
        MonitorSystemDetails? details = null;
        if (action == null) { return; }
        if (info == null) { return; }

        try
        {
            var toBeContinue = await Analyze(@event, action, null);
            if (!toBeContinue) { return; }

            var hookInstance = ServiceUtil.MonitorHooks.TryGetAndReturn(action.Hook);
            if (hookInstance == null)
            {
                _logger.LogWarning("hook {Hook} in monitor item id: {Id}, title: {Title} does not exist in service", action.Hook, action.Id, action.Title);
                return;
            }
            else
            {
                details = GetMonitorDetails(action, info, exception);
                _logger.LogInformation("monitor item id: {Id}, title: {Title} start to handle event {Event} with hook: {Hook} and distribution group {Group}", action.Id, action.Title, @event, action.Hook, action.Group.Name);
                await hookInstance.HandleSystem(details, cancellationToken);
                await SafeSaveMonitorAlert(action, details);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail to handle monitor item id: {Id}, title: {Title} with hook: {Hook}", action.Id, action.Title, action.Hook);
            await SafeSaveMonitorAlert(action, details, ex);
        }
    }

    private async Task<bool> Analyze(MonitorEvents @event, MonitorAction action, IJobExecutionContext? context)
    {
        if (@event == MonitorEvents.ExecutionSuccessWithNoEffectedRows)
        {
            var rows = ServiceUtil.GetEffectedRows(context);
            return rows != null && rows == 0;
        }

        if (MonitorEventsExtensions.IsSimpleJobMonitorEvent(@event))
        {
            return true;
        }

        if (MonitorEventsExtensions.IsSystemMonitorEvent(@event))
        {
            return true;
        }

        if (MonitorEventsExtensions.IsMonitorEventHasArguments(@event))
        {
            return await AnalyzeMonitorEventsWithArguments(@event, action, context);
        }

        return false;
    }

    private async Task<bool> AnalyzeMonitorEventsWithArguments(MonitorEvents @event, MonitorAction action, IJobExecutionContext? context)
    {
        if (context == null) { return false; } // analyze only for job execution (not for system execution)

        using var scope = serviceScopeFactory.CreateScope();
        var dal = scope.ServiceProvider.GetRequiredService<MonitorData>();
        var args = GetAndValidateArgs(action);
        args.JobId = JobKeyHelper.GetJobId(context.JobDetail);
        if (!args.Handle || args.Args == null) { return false; }

        switch (@event)
        {
            default:
                return false;

            case MonitorEvents.ExecutionFailxTimesInRow:
                if (args.JobId == null) { return false; }
                var count1 = await dal.CountFailsInRowForJob(new { args.JobId, Total = args.Args[0] });
                return count1 >= args.Args[0];

            case MonitorEvents.ExecutionFailxTimesInyHours:
                if (args.JobId == null) { return false; }
                var count2 = await dal.CountFailsInHourForJob(new { args.JobId, Hours = args.Args[1] });
                return count2 >= args.Args[0];

            case MonitorEvents.ExecutionEndWithEffectedRowsGreaterThanx:
                var rows = ServiceUtil.GetEffectedRows(context);
                return rows != null && rows > args.Args[0];

            case MonitorEvents.ExecutionEndWithEffectedRowsLessThanx:
                var rows1 = ServiceUtil.GetEffectedRows(context);
                return rows1 != null && rows1 < args.Args[0];

            case MonitorEvents.ExecutionEndWithEffectedRowsGreaterThanxInyHours:
                if (args.JobId == null) { return false; }
                var since1 = DateTime.Now.AddHours(-args.Args[1]);
                var er1 = await dal.SumEffectedRowsForJob(args.JobId, since1);
                return er1 > args.Args[0];

            case MonitorEvents.ExecutionEndWithEffectedRowsLessThanxInyHours:
                if (args.JobId == null) { return false; }
                var since2 = DateTime.Now.AddHours(-args.Args[1]);
                var er2 = await dal.SumEffectedRowsForJob(args.JobId, since2);
                return er2 < args.Args[0];

            case MonitorEvents.ExecutionDurationGreaterThanxMinutes:
                var duration = context.JobRunTime.TotalMinutes;
                return duration >= args.Args[0];
        }
    }

    private MonitorArguments GetAndValidateArgs(MonitorAction action)
    {
        var result = new MonitorArguments { Handle = true };

        try
        {
            var validator = new MonitorActionValidator(_logger);
            var args = validator.ValidateMonitorArguments(action);
            result.Args = args;
        }
        catch (RestValidationException)
        {
            return MonitorArguments.Empty;
        }

        return result;
    }

    private static MonitorDetails GetMonitorDetails(MonitorAction action, IJobExecutionContext context, Exception? exception)
    {
        var result = new MonitorDetails
        {
            Calendar = context.Trigger.CalendarName,
            Durable = context.JobDetail.Durable,
            FireInstanceId = context.FireInstanceId,
            FireTime = context.FireTimeUtc.LocalDateTime,
            JobDescription = context.JobDetail.Description,
            JobGroup = context.JobDetail.Key.Group,
            JobId = JobKeyHelper.GetJobId(context.JobDetail),
            Author = JobHelper.GetJobAuthor(context.JobDetail),
            JobName = context.JobDetail.Key.Name,
            JobRunTime = context.JobRunTime,
            MergedJobDataMap = Global.ConvertDataMapToDictionary(context.MergedJobDataMap),
            Recovering = context.JobDetail.RequestsRecovery,
            TriggerDescription = context.Trigger.Description,
            TriggerGroup = context.Trigger.Key.Group,
            TriggerId = TriggerHelper.GetTriggerId(context.Trigger),
            TriggerName = context.Trigger.Key.Name,
        };

        FillMonitor(result, action, exception);

        return result;
    }

    private static MonitorSystemDetails GetMonitorDetails(MonitorAction action, MonitorSystemInfo details, Exception? exception)
    {
        var result = new MonitorSystemDetails
        {
            MessageTemplate = details.MessageTemplate,
            MessagesParameters = details.MessagesParameters,
        };

        result.MessagesParameters ??= [];

        result.Message = result.MessageTemplate;
        foreach (var item in result.MessagesParameters)
        {
            result.Message = result.Message.Replace($"{{{{{item.Key}}}}}", item.Value);
        }

        FillMonitor(result, action, exception);
        return result;
    }

    private static void FillMonitor(Monitor.Monitor monitor, MonitorAction action, Exception? exception)
    {
        monitor.Users = [];
        monitor.EventId = action.EventId;
        monitor.EventTitle = MonitorUtil.GetMonitorEventTitle(action);
        monitor.Group = new MonitorGroup(action.Group);
        monitor.MonitorTitle = action.Title;
        monitor.Users.AddRange(action.Group.Users.Select(u => new MonitorUser(u)).ToList());
        monitor.GlobalConfig = Global.GlobalConfig;
        monitor.Environment = AppSettings.General.Environment;

        FillException(monitor, exception);
    }

    private static void FillException(Monitor.Monitor monitor, Exception? exception)
    {
        if (exception == null) { return; }
        if (exception is PlanarJobExecutionException jobException)
        {
            monitor.Exception = jobException.ExceptionText;
            monitor.MostInnerException = jobException.MostInnerExceptionText;
            monitor.MostInnerExceptionMessage = jobException.MostInnerMessage;
            return;
        }

        exception = GetTopRelevantException(exception);
        if (exception == null) { return; }

        monitor.Exception = exception.ToString();
        var inner = GetMostInnerException(exception);
        if (inner != null)
        {
            monitor.MostInnerException = inner.ToString();
            monitor.MostInnerExceptionMessage = inner.Message;
        }
    }

    private static Exception GetMostInnerException(Exception ex)
    {
        var innerException = ex;
        while (innerException.InnerException != null)
        {
            innerException = innerException.InnerException;
        }

        return innerException;
    }

    private static Exception? GetTopRelevantException(Exception ex)
    {
        var innerException = ex;
        do
        {
            if (IsRelevantException(innerException))
            {
                if (innerException.InnerException is TargetInvocationException)
                {
                    return innerException.InnerException.InnerException;
                }

                return innerException.InnerException;
            }
            innerException = innerException?.InnerException;
        } while (innerException != null);

        return ex;
    }

    private static bool IsRelevantException(Exception? ex)
    {
        const string source = $"{nameof(Planar)}.{nameof(Job)}";
        if (ex == null) { return false; }
        if (ex is AggregateException && ex.Source == source) { return true; }
        return false;
    }

    private async Task<bool> CheckForMutedMonitor(MonitorDetails? details, int monitorId)
    {
        if (details == null) { return false; }
        if (monitorId == 0) { return false; }

        try
        {
            if (details.JobId == null) { return false; }
            using var scope = serviceScopeFactory.CreateScope();
            var bl = scope.ServiceProvider.GetRequiredService<MonitorDomain>();
            return await bl.CheckForMutedMonitor(details.EventId, details.JobId, monitorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "fail to check monitor counter for monitor {Title} with event {Event}",
                details.MonitorTitle,
                details.EventTitle);
        }

        return false;
    }

    private async Task SafeSaveMonitorAlert(MonitorAction action, MonitorDetails? details, IJobExecutionContext context, Exception? exception = null)
    {
        try
        {
            if (details == null) { return; }
            if (action.Id == 0) { return; }

            var alert = new MonitorAlert();
            MapDetailsToMonitorAlert(details, alert);
            MapActionToMonitorAlert(action, alert);
            MapExceptionMonitorAlert(exception, alert);
            alert.LogInstanceId = context.FireInstanceId;

            using var scope = serviceScopeFactory.CreateScope();
            var dbcontext = scope.ServiceProvider.GetRequiredService<PlanarContext>();
            Console.WriteLine("SafeSaveMonitorAlert: " + Interlocked.Increment(ref _instanceCount));
            dbcontext.MonitorAlerts.Add(alert);
            await dbcontext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "fail to save monitor alert for monitor {Title} with event {Event}",
                details?.MonitorTitle ?? nullText,
                details?.EventTitle ?? nullText);
        }
    }

    private async Task SafeSaveMonitorAlert(MonitorAction action, MonitorSystemDetails? details, Exception? exception = null)
    {
        try
        {
            if (details == null) { return; }

            var alert = new MonitorAlert();
            MapDetailsToMonitorAlert(details, alert);
            MapActionToMonitorAlert(action, alert);
            MapExceptionMonitorAlert(exception, alert);

            using var scope = serviceScopeFactory.CreateScope();
            var dbcontext = scope.ServiceProvider.GetRequiredService<PlanarContext>();
            Console.WriteLine("SafeSaveMonitorAlert: " + Interlocked.Increment(ref _instanceCount));
            dbcontext.MonitorAlerts.Add(alert);
            await dbcontext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "fail to save monitor alert for monitor {Title} with event id {Id}",
                details?.MonitorTitle ?? nullText,
                details?.EventId ?? 0);
        }
    }

    private static void MapDetailsToMonitorAlert(MonitorDetails details, MonitorAlert alert)
    {
        MapMonitorToMonitorAlert(details, alert);
        alert.JobGroup = details.JobGroup;
        alert.JobName = details.JobName;
        alert.JobId = details.JobId;
        alert.AlertPayload = JsonConvert.SerializeObject(details);
    }

    private static void MapDetailsToMonitorAlert(MonitorSystemDetails details, MonitorAlert alert)
    {
        MapMonitorToMonitorAlert(details, alert);
        alert.AlertPayload = JsonConvert.SerializeObject(details);
    }

    private static void MapExceptionMonitorAlert(Exception? exception, MonitorAlert alert)
    {
        alert.Exception = exception?.ToString();
        alert.HasError = exception != null;
    }

    private static void MapMonitorToMonitorAlert(Monitor.Monitor monitor, MonitorAlert alert)
    {
        // alert.EventTitle => copy from action and not from monitor
        alert.AlertDate = DateTime.Now;
        alert.EventId = monitor.EventId;
        alert.MonitorTitle = monitor.MonitorTitle;
        alert.UsersCount = monitor.Users?.Count ?? 0;
    }

    private static void MapActionToMonitorAlert(MonitorAction action, MonitorAlert alert)
    {
        alert.GroupId = action.Group.Id;
        alert.GroupName = action.Group.Name;
        alert.MonitorId = action.Id;
        alert.Hook = action.Hook;
        alert.EventArgument = action.EventArgument;
        alert.EventTitle = MonitorUtil.GetMonitorEventTitle(action);
    }

    private async Task SaveMonitorCounter(MonitorAction action, MonitorDetails? details)
    {
        if (details == null) { return; }
        if (action.Id == 0) { return; }

        try
        {
            if (details.JobId == null) { return; }

            using var scope = serviceScopeFactory.CreateScope();
            var bl = scope.ServiceProvider.GetRequiredService<MonitorDomain>();
            await bl.SaveMonitorCounter(action, details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "fail to save monitor counter for monitor {Title} with event {Event}",
                details?.MonitorTitle ?? nullText,
                details?.EventTitle ?? nullText);
        }
    }

    #endregion Execute Monitor
}