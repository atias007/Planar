using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.API.Helpers;
using Planar.Service.General;
using Planar.Service.List.Base;
using Planar.Service.Monitor;
using Quartz;
using System;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using DbJobInstanceLog = Planar.Service.Model.JobInstanceLog;

namespace Planar.Service.List
{
    public class LogJobListener : BaseJobListenerWithDataLayer<LogJobListener>, IJobListener
    {
        public string Name => nameof(LogJobListener);

        public async Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            if (context.JobDetail.Key.Group == Consts.PlanarSystemGroup) { return; }

            try
            {
                await DAL.SetJobInstanceLogStatus(context.FireInstanceId, StatusMembers.Veto);
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
            if (context.JobDetail.Key.Group == Consts.PlanarSystemGroup) { return; }

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
                    JobId = JobKeyHelper.GetJobId(context.JobDetail),
                    JobName = context.JobDetail.Key.Name,
                    JobGroup = context.JobDetail.Key.Group,
                    TriggerId = TriggerKeyHelper.GetTriggerId(context.Trigger),
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
            if (context.JobDetail.Key.Group == Consts.PlanarSystemGroup) { return; }

            try
            {
                var duration = context.JobRunTime.TotalMilliseconds;
                var endDate = context.FireTimeUtc.ToLocalTime().DateTime.Add(context.JobRunTime);
                var status = jobException == null ? StatusMembers.Success : StatusMembers.Fail;

                var metadata = context.Result as JobExecutionMetadata;

                var log = new DbJobInstanceLog
                {
                    InstanceId = context.FireInstanceId,
                    Duration = Convert.ToInt32(duration),
                    EndDate = endDate,
                    Exception = jobException?.ToString(),
                    EffectedRows = metadata?.EffectedRows,
                    Information = metadata?.Information.ToString(),
                    Status = (int)status,
                    StatusTitle = status.ToString(),
                    IsStopped = context.CancellationToken.IsCancellationRequested
                };

                await DAL.UpdateHistoryJobRunLog(log);
            }
            catch (Exception ex)
            {
                var source = nameof(JobWasExecuted);
                Logger.LogCritical(ex, "Error handle {@source}: {@Message} ", source, ex.Message);
            }
            finally
            {
                await MonitorJobWasExecuted(context, jobException, cancellationToken);
            }
        }

        private static async Task MonitorJobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken)
        {
            await MonitorUtil.Scan(MonitorEvents.ExecutionEnd, context, jobException, cancellationToken);

            var @event =
                jobException == null ?
                MonitorEvents.ExecutionSuccess :
                MonitorEvents.ExecutionFail;

            await MonitorUtil.Scan(@event, context, jobException, cancellationToken);
        }

        private static string GetJobDataForLogging(JobDataMap data)
        {
            if (data.Count == 0) return null;

            var items = Global.ConvertDataMapToDictionary(data);
            var yml = new SerializerBuilder().Build().Serialize(items);
            return yml;
        }
    }
}