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
        private static readonly LazySingleton<List<MonitorAction>> _monitorData = new(() => { return LoadMonitor().Result; });
        private static readonly ILogger<MonitorUtil> _logger = Global.GetLogger<MonitorUtil>();

        public static void Load()
        {
            _monitorData.Reload();

            // TODO: warn for no items
        }

        public static IEnumerable<string> Hooks
        {
            get
            {
                return _monitorData.Instance.Select(m => m.Hook);
            }
        }

        public static void Validate<T>(ILogger<T> logger)
        {
            if (Count == 0)
            {
                logger.LogWarning("There is no monitor items. Service does not have any monitor");
            }

            var missingHooks = Hooks.Where(h => ServiceUtil.MonitorHooks.Keys.Contains(h) == false).ToList();
            missingHooks.ForEach(h => logger.LogWarning("Monitor with hook '{@h}' is invalid. Missing hook in service", h));
        }

        public static int Count
        {
            get
            {
                return _monitorData.Instance.Count;
            }
        }

        public static DataLayer DAL
        {
            get
            {
                try
                {
                    return Global.ServiceProvider.GetService(typeof(DataLayer)) as DataLayer;
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Error initialize DataLayer at BaseJobListenerWithDataLayer");
                    throw;
                }
            }
        }

        internal static async Task Scan(MonitorEvents @event, IJobExecutionContext context, JobExecutionException jobException = default, CancellationToken cancellationToken = default)
        {
            var task = Task.Run(() =>
            {
                if (context.JobDetail.Key.Group.StartsWith(Consts.PlanarSystemGroup))
                {
                    return;
                }

                List<MonitorAction> items;
                var hookTasks = new List<Task>();

                try
                {
                    items = LoadMonitorItems(@event, context);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fail to handle monitor item(s) --> LoadMonitorItems");
                    return;
                }

                Parallel.ForEach(items, action =>
                {
                    try
                    {
                        var toBeContinue = Analyze(@event, action).Result;
                        if (toBeContinue)
                        {
                            var hookInstance = GetMonitorHookInstance(action.Hook);
                            if (hookInstance == null)
                            {
                                _logger.LogWarning("Hook {@Hook} in monitor item id: {@Id}, title: '{@Title}' does not exist in service", action.Hook, action.Id, action.Title);
                            }
                            else
                            {
                                var details = GetMonitorDetails(action, context, jobException);
                                var hookType = ServiceUtil.MonitorHooks[action.Hook]?.Type;
                                var logger = Global.GetLogger(hookType);
                                var hookTask = hookInstance.Handle(details, _logger)
                                .ContinueWith(t =>
                                {
                                    if (t.Exception != null)
                                    {
                                        logger.LogError(t.Exception, "Fail to handle monitor item id: {Id}, title: '{Title}' with hook {Hook}", action.Id, action.Title, action.Hook);
                                    }
                                });

                                logger.LogInformation("Monitor item id: {Id}, title: '{Title}' start to handle with hook {Hook}", action.Id, action.Title, action.Hook);
                                hookTasks.Add(hookTask);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Fail to handle monitor item id: {Id}, title: '{Title}' with hook {Hook}", action.Id, action.Title, action.Hook);
                    }
                });

                try
                {
                    Task.WaitAll(hookTasks.ToArray());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fail to handle monitor item(s)");
                }
            }, cancellationToken);

            await task;
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

            result.Users.AddRange(action.Group.UsersToGroups.Select(ug => new MonitorUser(ug.User)).ToList());

            return result;
        }

        private static List<MonitorAction> LoadMonitorItems(MonitorEvents @event, IJobExecutionContext context)
        {
            var eventItems = _monitorData.Instance.Where(m => m.EventId == (int)@event && m.Active.GetValueOrDefault());

            var allJobsItems = eventItems.Where(m =>
                string.IsNullOrEmpty(m.JobGroup) &&
                string.IsNullOrEmpty(m.JobId));

            var jobGroupItems = eventItems.Where(m =>
                string.IsNullOrEmpty(m.JobGroup) == false &&
                string.Compare(m.JobGroup, context.JobDetail.Key.Group, true) == 0 &&
                string.IsNullOrEmpty(m.JobId));

            var jobId = JobKeyHelper.GetJobId(context.JobDetail);
            var jobIdItems = eventItems.Where(m =>
                string.IsNullOrEmpty(m.JobGroup) &&
                string.IsNullOrEmpty(m.JobId) == false &&
                string.Compare(m.JobId, jobId, true) == 0);

            var result = jobGroupItems.ToList();
            result.AddRange(jobIdItems.ToList());

            return result;
        }

        private static async Task<List<MonitorAction>> LoadMonitor()
        {
            var dal = MainService.Resolve<DataLayer>();
            return await dal.GetMonitorData();
        }

        private static async Task<bool> Analyze(MonitorEvents @event, MonitorAction action)
        {
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
                        _logger.LogWarning("Monitor action {@Id}, Title '{@Title}' has invalid argument ({@EventArgument}) or missing job id", action.Id, action.Title, action.EventArgument);
                        return false;
                    }

                    var count1 = await DAL.CountFailsInRowForJob(new { action.JobId, Total = args1 });
                    return count1 == args1;

                case MonitorEvents.ExecutionFailnTimesInHour:
                    _ = int.TryParse(action.EventArgument, out var args2);
                    if (args2 < 2 || string.IsNullOrEmpty(action.JobId))
                    {
                        _logger.LogWarning("Monitor action {@Id}, Title '{@Title}' has invalid argument ({@EventArgument}) or missing job id", action.Id, action.Title, action.EventArgument);
                        return false;
                    }

                    var count2 = await DAL.CountFailsInHourForJob(new { action.JobId });
                    return args2 >= count2;
            }
        }
    }
}