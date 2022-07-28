using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.General;
using Quartz;
using System;
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

        public override async Task<Empty> HealthCheck(Empty request, ServerCallContext context)
        {
            SchedulerUtil.HealthCheck(_logger);
            return await Task.FromResult(new Empty());
        }

        public override async Task<Empty> StopScheduler(Empty request, ServerCallContext context)
        {
            await SchedulerUtil.Stop(context.CancellationToken);
            return new Empty();
        }

        public override async Task<Empty> StartScheduler(Empty request, ServerCallContext context)
        {
            await SchedulerUtil.Start(context.CancellationToken);
            return new Empty();
        }

        public override async Task<IsJobRunningReply> IsJobRunning(RpcJobKey request, ServerCallContext context)
        {
            if (request == null)
            {
                throw new NullReferenceException();
            }

            var jobKey = new JobKey(request.Name, request.Group);
            var result = await SchedulerUtil.IsJobRunning(jobKey, context.CancellationToken);
            return new IsJobRunningReply { IsRunning = result };
        }

        public override async Task<RunningJobReply> GetRunningJob(GetRunningJobRequest request, ServerCallContext context)
        {
            var job = await SchedulerUtil.GetRunningJob(request.InstanceId, context.CancellationToken);
            var item = MapRunningJobReply(job);
            return item;
        }

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

        private static RunningJobReply MapRunningJobReply(RunningJobDetails job)
        {
            if (job == null) { return null; }

            var item = new RunningJobReply
            {
                Description = job.Description,
                EffectedRows = job.EffectedRows == null ? -1 : job.EffectedRows.Value,
                FireInstanceId = job.FireInstanceId,
                Group = job.Group,
                Id = job.Id,
                Name = job.Name,
                Progress = job.Progress,
                RunTime = job.RunTime,
                TriggerGroup = job.TriggerGroup,
                TriggerId = job.TriggerId,
                TriggerName = job.TriggerName,
                RefireCount = job.RefireCount,
                FireTime = Timestamp.FromDateTime(job.FireTime),
                NextFireTime = Timestamp.FromDateTime(job.NextFireTime.GetValueOrDefault()),
                PreviousFireTime = Timestamp.FromDateTime(job.PreviousFireTime.GetValueOrDefault()),
                ScheduledFireTime = Timestamp.FromDateTime(job.ScheduledFireTime.GetValueOrDefault()),
            };

            foreach (var d in job.DataMap)
            {
                item.DataMap.Add(new DataMap { Key = d.Key, Value = d.Value });
            }

            return item;
        }
    }
}