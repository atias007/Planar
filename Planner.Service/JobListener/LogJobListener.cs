using Microsoft.Extensions.Logging;
using Planner.API.Common.Entities;
using Planner.Service.JobListener.Base;
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
                await DAL.SetJobInstanceLogStatus(context.FireInstanceId, StatusMembers.Veto);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, ex, $"Error handle '{nameof(JobExecutionVetoed)}' at '{nameof(LogJobListener)}'");
            }
        }

        public async Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            try
            {
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
                if (log.Data.Length > 4000) { log.Data = log.Data[0..4000]; }
                if (log.JobId?.Length > 20) { log.JobId = log.JobId[0..20]; }
                if (log.JobName.Length > 50) { log.JobName = log.JobName[0..50]; }
                if (log.JobGroup.Length > 50) { log.JobGroup = log.JobGroup[0..50]; }
                if (log.TriggerId.Length > 20) { log.TriggerId = log.TriggerId[0..20]; }
                if (log.TriggerName.Length > 50) { log.TriggerName = log.TriggerName[0..50]; }
                if (log.TriggerGroup.Length > 50) { log.TriggerGroup = log.TriggerGroup[0..50]; }
                if (log.InstanceId.Length > 250) { log.InstanceId = log.InstanceId[0..250]; }

                await DAL.CreateJobInstanceLog(log);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, ex, $"Error handle '{nameof(JobToBeExecuted)}' at '{nameof(LogJobListener)}'");
            }
        }

        public async Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken = default)
        {
            try
            {
                var duration = context.JobRunTime.TotalMilliseconds;
                var endDate = context.FireTimeUtc.ToLocalTime().DateTime.Add(context.JobRunTime);
                var status = jobException == null ? StatusMembers.Success : StatusMembers.Fail;

                var metadata = JobExecutionMetadata.GetInstance(context);
                var log = new DbJobInstanceLog
                {
                    InstanceId = context.FireInstanceId,
                    Duration = Convert.ToInt32(duration),
                    EndDate = endDate,
                    Exception = jobException?.ToString(),
                    EffectedRows = metadata.EffectedRows,
                    Information = metadata.Information,
                    Status = (int)status,
                    StatusTitle = status.ToString(),
                    IsStopped = context.CancellationToken.IsCancellationRequested
                };

                await DAL.UpdateAutomationTaskCallLog(log);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, ex, $"Error handle '{nameof(JobWasExecuted)}' at '{nameof(LogJobListener)}'");
            }
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