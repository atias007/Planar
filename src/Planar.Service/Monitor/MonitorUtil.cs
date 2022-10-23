using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.API.Helpers;
using Planar.Service.Data;
using Planar.Service.General;
using Planar.Service.Model;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Monitor
{
    public class MonitorUtil
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
            var dal = scope.ServiceProvider.GetRequiredService<DataLayer>();
            var count = await dal.GetMonitorCount();
            if (count == 0)
            {
                _logger.LogWarning("There is no monitor items. Service does not have any monitor");
            }

            var hooks = await dal.GetMonitorHooks();
            var missingHooks = hooks.Where(h => ServiceUtil.MonitorHooks.ContainsKey(h) == false).ToList();
            missingHooks.ForEach(h => _logger.LogWarning("Monitor with hook '{Hook}' is invalid. Missing hook in service", h));
        }

        internal async Task Scan(MonitorEvents @event, IJobExecutionContext context, Exception exception = default, CancellationToken cancellationToken = default)
        {
            var task = Task.Run(() =>
            {
                ScanAsync(@event, context, exception, cancellationToken);
            }, cancellationToken);

            await task;
        }

        private Task ExecuteMonitor(MonitorAction action, MonitorEvents @event, IJobExecutionContext context, Exception exception)
        {
            Task hookTask = null;
            try
            {
                var toBeContinue = Analyze(@event, action).Result;
                if (toBeContinue)
                {
                    var hookInstance = GetMonitorHookInstance(action.Hook);
                    if (hookInstance == null)
                    {
                        _logger.LogWarning("Hook {Hook} in monitor item id: {Id}, title: '{Title}' does not exist in service", action.Hook, action.Id, action.Title);
                    }
                    else
                    {
                        var details = GetMonitorDetails(action, context, exception);
                        var hookType = ServiceUtil.MonitorHooks[action.Hook]?.Type;
                        hookTask = hookInstance.Handle(details, _logger)
                        .ContinueWith(t =>
                        {
                            if (t.Exception != null)
                            {
                                _logger.LogError(t.Exception, "Fail to handle monitor item id: {Id}, title: '{Title}' with hook {Hook}", action.Id, action.Title, action.Hook);
                            }
                        });

                        _logger.LogInformation("Monitor item id: {Id}, title: '{Title}' start to handle with hook {Hook}", action.Id, action.Title, action.Hook);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to handle monitor item id: {Id}, title: '{Title}' with hook {Hook}", action.Id, action.Title, action.Hook);
            }

            return hookTask;
        }

        internal void ScanAsync(MonitorEvents @event, IJobExecutionContext context, Exception exception = default, CancellationToken cancellationToken = default)
        {
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

            Parallel.ForEach(items, action =>
            {
                var task = ExecuteMonitor(action, @event, context, exception);
                if (task != null)
                {
                    hookTasks.Add(task);
                }
            });

            try
            {
                Task.WaitAll(hookTasks.ToArray(), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to handle monitor item(s)");
            }
        }

        private static HookInstance GetMonitorHookInstance(string hook)
        {
            var factory = ServiceUtil.MonitorHooks[hook];
            if (factory == null) { return null; }
            if (factory.Type == null) { return null; }

            var instance = Activator.CreateInstance(factory.Type);
            var method = instance.GetType().GetMethod("ExecuteHandle", BindingFlags.NonPublic | BindingFlags.Instance);
            var result = new HookInstance { Instance = instance, Method = method };
            return result;
        }

        private static MonitorDetails GetMonitorDetails(MonitorAction action, IJobExecutionContext context, Exception exception)
        {
            var result = new MonitorDetails
            {
                Calendar = context.Trigger.CalendarName,
                Durable = context.JobDetail.Durable,
                EventId = action.EventId,
                EventTitle = ((MonitorEvents)action.EventId).ToString(),
                FireInstanceId = context.FireInstanceId,
                FireTime = context.Trigger.FinalFireTimeUtc.GetValueOrDefault().LocalDateTime,
                Group = new MonitorGroup(action.Group),
                JobDescription = context.JobDetail.Description,
                JobGroup = context.JobDetail.Key.Group,
                JobId = JobKeyHelper.GetJobId(context.JobDetail),
                JobName = context.JobDetail.Key.Name,
                JobRunTime = context.JobRunTime,
                MergedJobDataMap = Global.ConvertDataMapToDictionary(context.MergedJobDataMap),
                MonitorTitle = action.Title,
                Recovering = context.JobDetail.RequestsRecovery,
                TriggerDescription = context.Trigger.Description,
                TriggerGroup = context.Trigger.Key.Group,
                TriggerId = TriggerKeyHelper.GetTriggerId(context.Trigger),
                TriggerName = context.Trigger.Key.Name,
                Exception = exception,
            };

            result.Users.AddRange(action.Group.Users.Select(u => new MonitorUser(u)).ToList());

            return result;
        }

        private async Task<List<MonitorAction>> LoadMonitorItems(MonitorEvents @event, IJobExecutionContext context)
        {
            var group = context.JobDetail.Key.Group;
            var job = JobKeyHelper.GetJobId(context.JobDetail);
            using var scope = _serviceScopeFactory.CreateScope();
            var dal = scope.ServiceProvider.GetRequiredService<DataLayer>();
            var result = await dal.GetMonitorData((int)@event, group, job);
            return result;
        }

        private async Task<bool> Analyze(MonitorEvents @event, MonitorAction action)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dal = scope.ServiceProvider.GetRequiredService<DataLayer>();

            switch (@event)
            {
                case MonitorEvents.ExecutionVetoed:
                case MonitorEvents.ExecutionRetry:
                case MonitorEvents.ExecutionFail:
                case MonitorEvents.ExecutionSuccess:
                case MonitorEvents.ExecutionStart:
                case MonitorEvents.ExecutionEnd:
                default:
                    return true;

                case MonitorEvents.ExecutionFailnTimesInRow:

                    _ = int.TryParse(action.EventArgument, out var args1);
                    if (args1 < 2 || string.IsNullOrEmpty(action.JobId))
                    {
                        _logger.LogWarning("Monitor action {Id}, Title '{Title}' has invalid argument ({EventArgument}) or missing job id", action.Id, action.Title, action.EventArgument);
                        return false;
                    }

                    var count1 = await dal.CountFailsInRowForJob(new { action.JobId, Total = args1 });
                    return count1 == args1;

                case MonitorEvents.ExecutionFailnTimesInHour:
                    _ = int.TryParse(action.EventArgument, out var args2);
                    if (args2 < 2 || string.IsNullOrEmpty(action.JobId))
                    {
                        _logger.LogWarning("Monitor action {Id}, Title '{Title}' has invalid argument ({EventArgument}) or missing job id", action.Id, action.Title, action.EventArgument);
                        return false;
                    }

                    var count2 = await dal.CountFailsInHourForJob(new { action.JobId });
                    return args2 >= count2;
            }
        }
    }
}