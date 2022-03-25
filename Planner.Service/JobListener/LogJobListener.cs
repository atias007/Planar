using Microsoft.Extensions.Logging;
using Planner.API.Common.Entities;
using Planner.Service.JobListener.Base;
using Planner.Service.Monitor;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using DbJobInstanceLog = Planner.Service.Model.JobInstanceLog;

namespace Planner.Service.JobListener
{
    public class LogJobListener : BaseJobListenerWithDataLayer<LogJobListener>, IJobListener
    {
        public string Name => nameof(LogJobListener);

        public async Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                if (context.JobDetail.Key.Group == Consts.PlannerSystemGroup) { return; }

                await DAL.SetJobInstanceLogStatus(context.FireInstanceId, StatusMembers.Veto);
                await MonitorUtil.Scan(MonitorEvents.ExecutionVetoed, context, null, cancellationToken);
            }
            catch (Exception ex)
            {
                var source = nameof(JobExecutionVetoed);
                Logger.LogCritical(ex, "Error handle {@source}: {@Message} ", source, ex.Message);
            }
            finally
            {
                await MonitorUtil.Scan(MonitorEvents.ExecutionVetoed, context, null, cancellationToken);
            }
        }

        public async Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                if (context.JobDetail.Key.Group == Consts.PlannerSystemGroup) { return; }

                string data = GetJobDataForLogging(context.MergedJobDataMap);

                var log = new DbJobInstanceLog
                {
                    InstanceId = context.FireInstanceId,
                    Data = data,
                    StartDate = context.FireTimeUtc.ToLocalTime().DateTime,
                    Status = (int)StatusMembers.Running,
                    StatusTitle = StatusMembers.Running.ToString(),
                    JobId = context.JobDetail.JobDataMap.GetString(Consts.JobId),
                    JobName = context.JobDetail.Key.Name,
                    JobGroup = context.JobDetail.Key.Group,
                    TriggerId = context.Trigger.JobDataMap.GetString(Consts.TriggerId),
                    TriggerName = context.Trigger.Key.Name,
                    TriggerGroup = context.Trigger.Key.Group,
                    Retry = context.Trigger.Key.Group == Consts.RetryTriggerGroup
                };

                if (log.TriggerId == null) { log.TriggerId = Consts.ManualTriggerId; }
                if (log.Data?.Length > 4000) { log.Data = log.Data[0..4000]; }
                if (log.JobId?.Length > 20) { log.JobId = log.JobId[0..20]; }
                if (log.JobName.Length > 50) { log.JobName = log.JobName[0..50]; }
                if (log.JobGroup.Length > 50) { log.JobGroup = log.JobGroup[0..50]; }
                if (log.TriggerId.Length > 20) { log.TriggerId = log.TriggerId[0..20]; }
                if (log.TriggerName.Length > 50) { log.TriggerName = log.TriggerName[0..50]; }
                if (log.TriggerGroup.Length > 50) { log.TriggerGroup = log.TriggerGroup[0..50]; }
                if (log.InstanceId.Length > 250) { log.InstanceId = log.InstanceId[0..250]; }

                await DAL.CreateJobInstanceLog(log);
                await MonitorUtil.Scan(MonitorEvents.ExecutionStart, context, null, cancellationToken);
            }
            catch (Exception ex)
            {
                var source = nameof(JobToBeExecuted);
                Logger.LogCritical(ex, "Error handle {@source}: {@Message} ", source, ex.Message);
            }
            finally
            {
                await MonitorUtil.Scan(MonitorEvents.ExecutionStart, context, null, cancellationToken);
            }
        }

        public async Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken = default)
        {
            try
            {
                if (context.JobDetail.Key.Group == Consts.PlannerSystemGroup) { return; }

                var duration = context.JobRunTime.TotalMilliseconds;
                var endDate = context.FireTimeUtc.ToLocalTime().DateTime.Add(context.JobRunTime);
                var status = jobException == null ? StatusMembers.Success : StatusMembers.Fail;

                var metadata = JobExecutionMetadataUtil.GetInstance(context);
                var log = new DbJobInstanceLog
                {
                    InstanceId = context.FireInstanceId,
                    Duration = Convert.ToInt32(duration),
                    EndDate = endDate,
                    Exception = jobException?.ToString(),
                    EffectedRows = metadata.EffectedRows,
                    Information = metadata.Information.ToString(),
                    Status = (int)status,
                    StatusTitle = status.ToString(),
                    IsStopped = context.CancellationToken.IsCancellationRequested
                };

                await DAL.UpdateAutomationTaskCallLog(log);
                await MonitorJobWasExecuted(context, jobException, cancellationToken);
            }
            catch (Exception ex)
            {
                var source = nameof(JobWasExecuted);
                Logger.LogCritical(ex, "Error handle {@source}: {@Message} ", source, ex.Message);
            }
            finally
            {
                await MonitorUtil.Scan(MonitorEvents.ExecutionEnd, context, jobException, cancellationToken);
                if (jobException == null)
                {
                    await MonitorUtil.Scan(MonitorEvents.ExecutionSuccess, context, jobException, cancellationToken);
                }
                else
                {
                    await MonitorUtil.Scan(MonitorEvents.ExecutionFail, context, jobException, cancellationToken);
                }
            }
        }

        private static async Task MonitorJobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken)
        {
            var task1 = MonitorUtil.Scan(MonitorEvents.ExecutionEnd, context, jobException, cancellationToken);

            var @event =
                jobException == null ?
                MonitorEvents.ExecutionSuccess :
                MonitorEvents.ExecutionFail;

            var task2 = MonitorUtil.Scan(@event, context, jobException, cancellationToken);

            await Task.WhenAll(task1, task2);
        }

        private static string GetJobDataForLogging(JobDataMap data)
        {
            if (data.Count == 0) return null;

            var items = new Dictionary<string, string>();
            foreach (var item in data)
            {
                items.Add(item.Key, Convert.ToString(item.Value));
            }

            var final = items
                .Where(i => i.Key.StartsWith(Consts.QuartzPrefix) == false && i.Key.StartsWith(Consts.ConstPrefix) == false)
                .ToDictionary(i => i.Key, i => i.Value);
            var yml = new SerializerBuilder().Build().Serialize(final);
            return yml;
        }
    }
}