using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
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
    }
}