using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Common.Exceptions;
using Planar.Common.Helpers;
using Planar.Service.API.Helpers;
using Planar.Service.Data;
using Planar.Service.General;
using Planar.Service.Model;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Planar.Service.Monitor
{
    public class MonitorUtil : IMonitorUtil
    {
        private readonly ILogger<MonitorUtil> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public MonitorUtil(IServiceScopeFactory serviceScopeFactory, ILogger<MonitorUtil> logger)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task Validate()
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dal = scope.ServiceProvider.GetRequiredService<MonitorData>();
            var count = await dal.GetMonitorCount();
            if (count == 0)
            {
                _logger.LogWarning("There is no monitor items. Service does not have any monitor");
            }

            var hooks = await dal.GetMonitorUsedHooks();
            var missingHooks = hooks.Where(h => !ServiceUtil.MonitorHooks.ContainsKey(h)).ToList();
            missingHooks.ForEach(h => _logger.LogWarning("Monitor with hook '{Hook}' is invalid. Missing hook in service", h));
        }

        public async Task Scan(MonitorEvents @event, IJobExecutionContext context, Exception? exception = default)
        {
            if (context == null)
            {
                _logger.LogWarning($"IJobExecutionContext is null in {nameof(MonitorUtil)}.{nameof(MonitorUtil.Scan)}. Scan skipped");
                return;
            }

            if (context.JobDetail.Key.Group.StartsWith(Consts.PlanarSystemGroup))
            {
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
                _logger.LogError(ex, "Fail to handle monitor item(s) --> LoadMonitorItems");
                return;
            }

            foreach (var action in items)
            {
                var task = ExecuteMonitor(action, @event, context, exception);
                hookTasks.Add(task);
            }

            try
            {
                await Task.WhenAll(hookTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to handle monitor item(s)");
            }
        }

        public async Task Scan(MonitorEvents @event, MonitorSystemInfo info, Exception? exception = default)
        {
            List<MonitorAction> items;
            var hookTasks = new List<Task>();

            try
            {
                items = await LoadMonitorItems(@event);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to handle monitor item(s) --> LoadMonitorItems");
                return;
            }

            foreach (var action in items)
            {
                var task = ExecuteMonitor(action, @event, info, exception);
                hookTasks.Add(task);
            }

            try
            {
                await Task.WhenAll(hookTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to handle monitor item(s)");
            }
        }

        public async Task<ExecuteMonitorResult> ExecuteMonitor(MonitorAction action, MonitorEvents @event, IJobExecutionContext context, Exception? exception)
        {
            try
            {
                var toBeContinue = await Analyze(@event, action, context);
                if (!toBeContinue) { return ExecuteMonitorResult.Ok; }

                var hookInstance = GetMonitorHookInstance(action.Hook);
                if (hookInstance == null)
                {
                    _logger.LogWarning("Hook {Hook} in monitor item id: {Id}, title: '{Title}' does not exist in service", action.Hook, action.Id, action.Title);
                    var message = $"Hook {action.Hook} in monitor item id: {action.Id}, title: '{action.Title}' does not exist in service";
                    return ExecuteMonitorResult.Fail(message);
                }
                else
                {
                    var details = GetMonitorDetails(action, context, exception);
                    if (@event == MonitorEvents.ExecutionProgressChanged)
                    {
                        _logger.LogDebug("Monitor item id: {Id}, title: '{Title}' start to handle event {Event} with hook: {Hook} and distribution group '{Group}'", action.Id, action.Title, @event, action.Hook, action.Group.Name);
                    }
                    else
                    {
                        _logger.LogInformation("Monitor item id: {Id}, title: '{Title}' start to handle event {Event} with hook: {Hook} and distribution group '{Group}'", action.Id, action.Title, @event, action.Hook, action.Group.Name);
                    }

                    await hookInstance.Handle(details, _logger);
                    return ExecuteMonitorResult.Ok;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to handle monitor item id: {Id}, title: '{Title}' with hook: {Hook} and distribution group '{Group}'", action.Id, action.Title, action.Hook, action.Group.Name);
                var message = $"Fail to handle monitor item id: {action.Id}, title: '{action.Title}' with hook: {action.Hook}. Error message: {ex.Message}";
                return ExecuteMonitorResult.Fail(message);
            }
        }

        public async Task<ExecuteMonitorResult> ExecuteMonitor(MonitorAction action, MonitorEvents @event, MonitorSystemInfo info, Exception? exception)
        {
            try
            {
                var toBeContinue = await Analyze(@event, action, null);
                if (!toBeContinue) { return ExecuteMonitorResult.Ok; }

                var hookInstance = GetMonitorHookInstance(action.Hook);
                if (hookInstance == null)
                {
                    _logger.LogWarning("Hook {Hook} in monitor item id: {Id}, title: '{Title}' does not exist in service", action.Hook, action.Id, action.Title);
                    var message = $"Hook {action.Hook} in monitor item id: {action.Id}, title: '{action.Title}' does not exist in service";
                    return ExecuteMonitorResult.Fail(message);
                }
                else
                {
                    var details = GetMonitorDetails(action, info, exception);
                    _logger.LogInformation("Monitor item id: {Id}, title: '{Title}' start to handle event {Event} with hook: {Hook} and distribution group '{Group}'", action.Id, action.Title, @event, action.Hook, action.Group.Name);
                    await hookInstance.HandleSystem(details, _logger);
                    return ExecuteMonitorResult.Ok;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to handle monitor item id: {Id}, title: '{Title}' with hook: {Hook}", action.Id, action.Title, action.Hook);
                var message = $"Fail to handle monitor item id: {action.Id}, title: '{action.Title}' with hook: {action.Hook}. Error message: {ex.Message}";
                return ExecuteMonitorResult.Fail(message);
            }
        }

        private static HookInstance? GetMonitorHookInstance(string hook)
        {
            var factory = ServiceUtil.MonitorHooks[hook];
            if (factory == null) { return null; }
            if (factory.Type == null) { return null; }

            var instance = Activator.CreateInstance(factory.Type);
            if (instance == null) { return null; }

            var method1 = SafeGetMethod(hook, HookInstance.HandleMethodName, instance);
            var method2 = SafeGetMethod(hook, HookInstance.HandleSystemMethodName, instance);

            var result = new HookInstance { Instance = instance, HandleMethod = method1, HandleSystemMethod = method2 };
            return result;
        }

        private static MethodInfo SafeGetMethod(string hook, string methodName, object instance)
        {
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
            var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
            return method ?? throw new PlanarException($"method {methodName} could not found in hook {hook}");
        }

        private static MonitorDetails GetMonitorDetails(MonitorAction action, IJobExecutionContext context, Exception? exception)
        {
            // ****** ATTENTION: any changes should reflect in TestJobExecutionContext ******
            var result = new MonitorDetails
            {
                Calendar = context.Trigger.CalendarName,
                Durable = context.JobDetail.Durable,
                FireInstanceId = context.FireInstanceId,
                FireTime = context.Trigger.FinalFireTimeUtc.GetValueOrDefault().LocalDateTime,
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

            // ****** ATTENTION: any changes should reflect in TestJobExecutionContext ******
        }

        private static MonitorSystemDetails GetMonitorDetails(MonitorAction action, MonitorSystemInfo details, Exception? exception)
        {
            var result = new MonitorSystemDetails
            {
                MessageTemplate = details.MessageTemplate,
                MessagesParameters = details.MessagesParameters,
            };

            result.MessagesParameters ??= new();

            result.Message = result.MessageTemplate;
            foreach (var item in result.MessagesParameters)
            {
                result.Message = result.Message.Replace($"{{{{{item.Key}}}}}", item.Value);
            }

            FillMonitor(result, action, exception);
            return result;
        }

        private static void FillMonitor(Monitor monitor, MonitorAction action, Exception? exception)
        {
            monitor.Users = new List<MonitorUser>();
            monitor.EventId = action.EventId;
            monitor.EventTitle = ((MonitorEvents)action.EventId).ToString();
            monitor.Group = new MonitorGroup(action.Group);
            monitor.MonitorTitle = action.Title;
            monitor.Users.AddRange(action.Group.Users.Select(u => new MonitorUser(u)).ToList());
            monitor.GlobalConfig = Global.GlobalConfig;

            FillException(monitor, exception);
        }

        private static void FillException(Monitor monitor, Exception? exception)
        {
            if (exception == null) { return; }
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

        private static Exception GetMostInnerException(Exception ex)
        {
            var innerException = ex;
            while (innerException.InnerException != null)
            {
                innerException = innerException.InnerException;
            }

            return innerException;
        }

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
            using var scope = _serviceScopeFactory.CreateScope();
            var dal = scope.ServiceProvider.GetRequiredService<MonitorData>();
            var data = await dal.GetMonitorDataByEvent(@event);
            return data;
        }

        private async Task<List<MonitorAction>> GetMonitorDataByGroup(int @event, string jobGroup)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dal = scope.ServiceProvider.GetRequiredService<MonitorData>();
            var data = await dal.GetMonitorDataByGroup(@event, jobGroup);
            return data;
        }

        private async Task<List<MonitorAction>> GetMonitorDataByJob(int @event, string jobGroup, string jobName)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dal = scope.ServiceProvider.GetRequiredService<MonitorData>();
            var data = await dal.GetMonitorDataByJob(@event, jobGroup, jobName);
            return data;
        }

        private async Task<MonitorArguments> GetAndValidateArgs(MonitorAction action, JobKeyHelper jobKeyHelper)
        {
            _ = int.TryParse(action.EventArgument, out var args);
            var jobId = await jobKeyHelper.GetJobId(action);
            if (args < 2 || string.IsNullOrEmpty(jobId))
            {
                _logger.LogWarning("Monitor action {Id}, Title '{Title}' has invalid argument ({EventArgument}) or missing job group/name", action.Id, action.Title, action.EventArgument);
                return MonitorArguments.Empty;
            }

            var result = new MonitorArguments { Arg = args, Handle = true, JobId = jobId };
            return result;
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
            using var scope = _serviceScopeFactory.CreateScope();
            var jobKeyHelper = scope.ServiceProvider.GetRequiredService<JobKeyHelper>();
            var dal = scope.ServiceProvider.GetRequiredService<MonitorData>();
            var args = await GetAndValidateArgs(action, jobKeyHelper);
            if (!args.Handle) { return false; }

            switch (@event)
            {
                default:
                    return false;

                case MonitorEvents.ExecutionFailxTimesInRow:
                    var count1 = await dal.CountFailsInRowForJob(new { args.JobId, Total = args.Arg });
                    return count1 >= args.Arg;

                case MonitorEvents.ExecutionFailxTimesInHour:
                    var count2 = await dal.CountFailsInHourForJob(new { args.JobId });
                    return count2 >= args.Arg;

                case MonitorEvents.ExecutionEndWithEffectedRowsGreaterThanx:
                    var rows = ServiceUtil.GetEffectedRows(context);
                    return rows != null && rows > args.Arg;

                case MonitorEvents.ExecutionEndWithEffectedRowsLessThanx:
                    var rows1 = ServiceUtil.GetEffectedRows(context);
                    return rows1 != null && rows1 < args.Arg;
            }
        }
    }
}