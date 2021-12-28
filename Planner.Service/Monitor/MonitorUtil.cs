using Microsoft.Extensions.Logging;
using Planner.Common;
using Planner.MonitorHook;
using Planner.Service.Data;
using Planner.Service.General;
using Planner.Service.Model;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planner.Service.Monitor
{
    public class MonitorUtil
    {
        private static readonly LazySingleton<List<MonitorAction>> _monitorData = new(() => { return LoadMonitor().Result; });

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
            missingHooks.ForEach(h => logger.LogWarning($"Monitor with hook '{h}' is invalid. Missing hook in service"));
        }

        public static int Count
        {
            get
            {
                return _monitorData.Instance.Count;
            }
        }

        public static async Task Scan(MonitorEvents @event, IJobExecutionContext context, JobExecutionException jobException = default, CancellationToken cancellationToken = default)
        {
            var task = Task.Run(() =>
            {
                var logger = Global.GetLogger<MonitorUtil>();
                var hookTasks = new List<Task>();
                var items = LoadMonitorItems(@event, context);
                Parallel.ForEach(items, action =>
                {
                    try
                    {
                        var hook = GetMonitorHook(action.Hook);
                        if (hook == null)
                        {
                            logger.LogWarning($"Hook '{action.Hook}' in monitor item id: {action.Id}, title: '{action.Title}' is not exists in service");
                        }
                        else
                        {
                            var details = GetMonitorDetails(action, context, jobException);
                            var hookType = ServiceUtil.MonitorHooks[action.Hook];
                            var logger = Global.GetLogger(hookType);
                            var hookTask = hook.Handle(details, logger)
                            .ContinueWith(t =>
                            {
                                if (t.Exception != null)
                                {
                                    logger.LogError(t.Exception, $"Fail to handle monitor item id: {action.Id}, title: '{action.Title}' with hook '{action.Hook}'");
                                }
                            });

                            logger.LogInformation($"Monitor item id: {action.Id}, title: '{action.Title}' start to handle with hook '{action.Hook}'");
                            hookTasks.Add(hookTask);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Fail to handle monitor item id: {action.Id}, title: '{action.Title}' with hook '{action.Hook}'");
                    }
                });

                try
                {
                    Task.WaitAll(hookTasks.ToArray());
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Fail to handle monitor item(s)");
                }
            }, cancellationToken);

            await task;
        }

        private static IMonitorHook GetMonitorHook(string hook)
        {
            var result = ServiceUtil.MonitorHooks[hook];
            if (result == null) { return null; }

            var instance = Activator.CreateInstance(result) as IMonitorHook;
            return instance;
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
                JobId = Convert.ToString(context.JobDetail.JobDataMap.Get(Consts.JobId)),
                JobName = context.JobDetail.Key.Name,
                JobRunTime = context.JobRunTime,
                MergedJobDataMap = ServiceUtil.ConvertJobDataMapToDictionary(context.MergedJobDataMap),
                MonitorTitle = action.Title,
                Recovering = context.JobDetail.RequestsRecovery,
                TriggerDescription = context.Trigger.Description,
                TriggerGroup = context.Trigger.Key.Group,
                TriggerId = Convert.ToString(context.Trigger.JobDataMap.Get(Consts.TriggerId)),
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

            var jobId = Convert.ToString(context.JobDetail.JobDataMap[Consts.JobId]);
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
    }
}