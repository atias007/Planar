﻿using Microsoft.Extensions.DependencyInjection;
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
using System.Collections.Concurrent;
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
    private static readonly ConcurrentDictionary<string, DateTimeOffset> _lockJobEvents = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reader = _channel.Reader;
        while (!reader.Completion.IsCompleted && await reader.WaitToReadAsync(stoppingToken))
        {
            if (!reader.TryRead(out var monitor)) { continue; }

            switch (monitor.Type)
            {
                case MonitorScanType.ScanJob:
                    await SafeScanInner(monitor.Event, monitor.JobExecutionContext, monitor.Exception, stoppingToken);
                    break;

                case MonitorScanType.ScanSystem:
                    await SafeScanInner(monitor.Event, monitor.MonitorSystemInfo, monitor.Exception, stoppingToken);
                    break;

                case MonitorScanType.ExecuteJob:
                    await ExecuteMonitor(monitor.MonitorAction, monitor.Event, monitor.JobExecutionContext, monitor.Exception, stoppingToken);
                    break;

                case MonitorScanType.ExecuteSystem:
                    await ExecuteMonitor(monitor.MonitorAction, monitor.Event, monitor.MonitorSystemInfo, monitor.Exception, stoppingToken);
                    break;

                case MonitorScanType.Lock:
                    LockJobOrTriggerEvent(monitor.LockKey, monitor.LockSeconds);
                    break;

                default:
                    break;
            }
        }

        _channel.Writer.TryComplete();
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
            _logger.LogWarning($"MonitorSystemInfo is null in {nameof(MonitorService)}.{nameof(ScanInner)}. Scan skipped");
            return;
        }

        List<MonitorAction> items;
        var hookTasks = new List<Task>();

        if (IsJobOrTriggerEventLock(info, @event)) { return; }

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
            var task = ExecuteMonitor(action, @event, info, exception, cancellationToken);
            hookTasks.Add(task);
        }

        try
        {
            await Task.WhenAll(hookTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail to handle monitor item(s)");
        }
    }

    private async Task ScanInner(MonitorEvents @event, IJobExecutionContext? context, Exception? exception = default, CancellationToken cancellationToken = default)
    {
        if (context == null)
        {
            _logger.LogWarning($"IJobExecutionContext is null in {nameof(MonitorService)}.{nameof(ScanInner)}. Scan skipped");
            return;
        }

        if (context.JobDetail.Key.Group.StartsWith(Consts.PlanarSystemGroup))
        {
            return;
        }

        if (IsJobOrTriggerEventLock(context.JobDetail.Key, @event))
        {
            ReleaseJobOrTriggerEvent(context.JobDetail.Key, @event);
            return;
        }

        List<MonitorAction> items;
        var hookTasks = new List<Task>();

        try
        {
            items = LoadMonitorItems(@event, context).Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail to handle monitor item(s) --> LoadMonitorItems");
            return;
        }

        foreach (var action in items)
        {
            var task = ExecuteMonitor(action, @event, context, exception, cancellationToken);
            hookTasks.Add(task);
        }

        try
        {
            await Task.WhenAll(hookTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail to handle monitor item(s)");
        }
    }

    #region Lock Event

    private static void LockJobOrTriggerEvent(string? key, int? lockSeconds)
    {
        if (string.IsNullOrWhiteSpace(key) || lockSeconds.GetValueOrDefault() <= 0) { return; }
        _lockJobEvents.TryAdd(key, DateTimeOffset.Now);
        Task.Run(() =>
        {
            Thread.Sleep(lockSeconds.GetValueOrDefault() * 1000);
            _lockJobEvents.TryRemove(key, out _);
        });
    }

    private static bool IsJobOrTriggerEventLock<T>(Key<T> key, MonitorEvents @event)
    {
        var keyString = $"{key} {@event}";
        return _lockJobEvents.ContainsKey(keyString);
    }

    private static bool IsJobOrTriggerEventLock(MonitorSystemInfo info, MonitorEvents @event)
    {
        var keyString = string.Empty;
        var hasGroup = info.MessagesParameters.TryGetValue("JobGroup", out var jobGroup);
        var hasName = info.MessagesParameters.TryGetValue("JobName", out var jobName);
        if (hasGroup && hasName)
        {
            keyString = $"{jobGroup}.{jobName} {@event}";
        }

        hasGroup = info.MessagesParameters.TryGetValue("TriggerGroup", out var triggerGroup);
        hasName = info.MessagesParameters.TryGetValue("TriggerName", out var triggerName);
        if (hasGroup && hasName)
        {
            keyString = $"{triggerGroup}.{triggerName} {@event}";
        }

        if (string.IsNullOrEmpty(keyString))
        {
            return false;
        }

        return _lockJobEvents.ContainsKey(keyString);
    }

    private static void ReleaseJobOrTriggerEvent<T>(Key<T> key, MonitorEvents @event)
    {
        var keyString = $"{key} {@event}";
        _lockJobEvents.TryRemove(keyString, out _);
    }

    #endregion Lock Event

    #region Load Monitor Items

    private async Task<List<MonitorAction>> LoadMonitorItems(MonitorEvents @event, IJobExecutionContext context)
    {
        var key = context.JobDetail.Key;

        var task1 = GetMonitorDataByEvent((int)@event);
        var task2 = GetMonitorDataByGroup((int)@event, key.Group);
        var task3 = GetMonitorDataByJob((int)@event, key.Group, key.Name);

        await Task.WhenAll(task1, task2, task3);

        var result = task1.Result
            .Union(task2.Result)
            .Union(task3.Result)
            .Distinct()
            .ToList();

        return result;
    }

    private async Task<List<MonitorAction>> LoadMonitorItems(MonitorEvents @event)
    {
        var result = await GetMonitorDataByEvent((int)@event);
        return result;
    }

    private async Task<List<MonitorAction>> GetMonitorDataByEvent(int @event)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dal = scope.ServiceProvider.GetRequiredService<MonitorData>();
        var data = await dal.GetMonitorDataByEvent(@event);
        return data;
    }

    private async Task<List<MonitorAction>> GetMonitorDataByGroup(int @event, string jobGroup)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dal = scope.ServiceProvider.GetRequiredService<MonitorData>();
        var data = await dal.GetMonitorDataByGroup(@event, jobGroup);
        return data;
    }

    private async Task<List<MonitorAction>> GetMonitorDataByJob(int @event, string jobGroup, string jobName)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dal = scope.ServiceProvider.GetRequiredService<MonitorData>();
        var data = await dal.GetMonitorDataByJob(@event, jobGroup, jobName);
        return data;
    }

    #endregion Load Monitor Items

    #region Execute Monitor

    private async Task ExecuteMonitor(MonitorAction? action, MonitorEvents @event, IJobExecutionContext? context, Exception? exception, CancellationToken cancellationToken)
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
                await SaveMonitorAlert(action, details, context);

                // Save the monitor counter
                await SaveMonitorCounter(action, details);

                return;
            }
        }
        catch (Exception ex)
        {
            await SaveMonitorAlert(action, details, context, ex);
            _logger.LogError(ex, "fail to handle monitor item id: {Id}, title: {Title} with hook: {Hook} and distribution group {Group}", action.Id, action.Title, action.Hook, action.Group.Name);
        }
    }

    private async Task ExecuteMonitor(MonitorAction? action, MonitorEvents @event, MonitorSystemInfo? info, Exception? exception, CancellationToken cancellationToken)
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
                await SaveMonitorAlert(action, details);
                return;
            }
        }
        catch (Exception ex)
        {
            await SaveMonitorAlert(action, details, ex);
            _logger.LogError(ex, "fail to handle monitor item id: {Id}, title: {Title} with hook: {Hook}", action.Id, action.Title, action.Hook);
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

    private async Task SaveMonitorAlert(MonitorAction action, MonitorDetails? details, IJobExecutionContext context, Exception? exception = null)
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
            dbcontext.MonitorAlerts.Add(alert);
            await dbcontext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "fail to save monitor alert for monitor {Title} with event {Event}",
                details?.MonitorTitle ?? "[null]",
                details?.EventTitle ?? "[null]");
        }
    }

    private async Task SaveMonitorAlert(MonitorAction action, MonitorSystemDetails? details, Exception? exception = null)
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
            dbcontext.MonitorAlerts.Add(alert);
            await dbcontext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "fail to save monitor alert for monitor {Title} with event id {Id}",
                details?.MonitorTitle ?? "[null]",
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
                details?.MonitorTitle ?? "[null]",
                details?.EventTitle ?? "[null]");
        }
    }

    #endregion Execute Monitor
}