using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.General;
using Quartz;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Planar
{
    internal class ClusterService : PlanarCluster.PlanarClusterBase
    {
        private readonly ILogger<ClusterService> _logger;

        public ClusterService(ILogger<ClusterService> logger)
        {
            _logger = logger;
        }

        // OK
        public override async Task<Empty> HealthCheck(Empty request, ServerCallContext context)
        {
            SchedulerUtil.HealthCheck(_logger);
            return await Task.FromResult(new Empty());
        }

        // OK
        public override async Task<Empty> StopScheduler(Empty request, ServerCallContext context)
        {
            await SchedulerUtil.Stop(context.CancellationToken);
            return new Empty();
        }

        // OK
        public override async Task<Empty> StartScheduler(Empty request, ServerCallContext context)
        {
            await SchedulerUtil.Start(context.CancellationToken);
            return new Empty();
        }

        // OK
        public override async Task<IsJobRunningReply> IsJobRunning(RpcJobKey request, ServerCallContext context)
        {
            ValidateRequest(request);

            var jobKey = new JobKey(request.Name, request.Group);
            var result = await SchedulerUtil.IsJobRunning(jobKey, context.CancellationToken);
            return new IsJobRunningReply { IsRunning = result };
        }

        // OK
        public override async Task<RunningJobReply> GetRunningJob(GetRunningJobRequest request, ServerCallContext context)
        {
            ValidateRequest(request);
            var job = await SchedulerUtil.GetRunningJob(request.InstanceId, context.CancellationToken);
            var item = MapRunningJobReply(job);
            return item;
        }

        // OK
        public override async Task<GetRunningJobsReply> GetRunningJobs(Empty request, ServerCallContext context)
        {
            var result = new GetRunningJobsReply();
            var jobs = await SchedulerUtil.GetRunningJobs(context.CancellationToken);

            foreach (var j in jobs)
            {
                var item = MapRunningJobReply(j);
                result.Jobs.Add(item);
            }

            return result;
        }

        // OK
        public override async Task<RunningInfoReply> GetRunningInfo(GetRunningJobRequest request, ServerCallContext context)
        {
            ValidateRequest(request);
            var job = await SchedulerUtil.GetRunningInfo(request.InstanceId, context.CancellationToken);
            if (job == null) { return null; }

            var result = new RunningInfoReply
            {
                Exceptions = SafeString(job.Exceptions),
                Information = SafeString(job.Information)
            };

            return result;
        }

        // OK
        public override async Task<IsRunningInstanceExistReply> IsRunningInstanceExist(GetRunningJobRequest request, ServerCallContext context)
        {
            ValidateRequest(request);
            var result = await SchedulerUtil.IsRunningInstanceExistOnLocal(request.InstanceId, context.CancellationToken);
            return new IsRunningInstanceExistReply { Exists = result };
        }

        // OK
        public override async Task<StopRunningJobReply> StopRunningJob(GetRunningJobRequest request, ServerCallContext context)
        {
            ValidateRequest(request);
            var result = await SchedulerUtil.StopRunningJob(request.InstanceId, context.CancellationToken);
            return new StopRunningJobReply { IsStopped = result };
        }

        public override async Task<PersistanceRunningJobInfoReply> GetPersistanceRunningJobInfo(Empty request, ServerCallContext context)
        {
            var result = new PersistanceRunningJobInfoReply();
            var jobs = await SchedulerUtil.GetPersistanceRunningJobsInfo();
            var items = jobs.Select(j => new PersistanceRunningJobInfo
            {
                Exceptions = SafeString(j.Exceptions),
                Group = SafeString(j.Group),
                Information = SafeString(j.Information),
                InstanceId = SafeString(j.InstanceId),
                Name = SafeString(j.Name),
            });

            result.RunningJobs.AddRange(items);
            return result;
        }

        private static void ValidateRequest(object request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
        }

        private static string SafeString(string value)
        {
            if (value == null) { return string.Empty; }
            return value;
        }

        private static RunningJobReply MapRunningJobReply(RunningJobDetails job)
        {
            if (job == null) { return null; }

            var item = new RunningJobReply
            {
                Description = SafeString(job.Description),
                EffectedRows = job.EffectedRows == null ? -1 : job.EffectedRows.Value,
                FireInstanceId = SafeString(job.FireInstanceId),
                Group = SafeString(job.Group),
                Id = job.Id,
                Name = SafeString(job.Name),
                Progress = job.Progress,
                RunTime = Duration.FromTimeSpan(job.RunTime),
                TriggerGroup = SafeString(job.TriggerGroup),
                TriggerId = SafeString(job.TriggerId),
                TriggerName = SafeString(job.TriggerName),
                RefireCount = job.RefireCount,
                FireTime = GetTimeStamp(job.FireTime),
                NextFireTime = GetTimeStamp(job.NextFireTime),
                PreviousFireTime = GetTimeStamp(job.PreviousFireTime),
                ScheduledFireTime = GetTimeStamp(job.ScheduledFireTime),
            };

            foreach (var d in job.DataMap)
            {
                item.DataMap.Add(new DataMap { Key = d.Key, Value = d.Value });
            }

            return item;
        }

        private static Timestamp GetTimeStamp(DateTime date)
        {
            if (date == default) { return default; }
            var offset = new DateTimeOffset(date);
            var timestamp = Timestamp.FromDateTimeOffset(offset);
            return timestamp;
        }

        private static Timestamp GetTimeStamp(DateTime? date)
        {
            if (date == null) { return default; }
            if (date.Value == DateTime.MinValue) { return default; }
            var offset = new DateTimeOffset(date.Value);
            var timestamp = Timestamp.FromDateTimeOffset(offset);
            return timestamp;
        }
    }
}