using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.Data;
using Planar.Service.General;
using Planar.Service.Model;
using Quartz;
using System;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs
{
    [DisallowConcurrentExecution]
    public class ClusterHealthCheckJob : IJob
    {
        private readonly ILogger<ClusterHealthCheckJob> _logger;

        private readonly DataLayer _dal;

        public ClusterHealthCheckJob()
        {
            _logger = Global.GetLogger<ClusterHealthCheckJob>();
            _dal = Global.ServiceProvider.GetService<DataLayer>();
        }

        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                return DoWork();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail check health of cluster: {Message}", ex.Message);
                return Task.CompletedTask;
            }
        }

        private async Task DoWork()
        {
            var currentNode = new ClusterNode
            {
                Port = AppSettings.HttpPort,
                Server = Environment.MachineName,
                InstanceId = MainService.Scheduler.SchedulerInstanceId
            };

            var nodes = await _dal.GetClusterNodes();
            foreach (var node in nodes)
            {
                if (node == currentNode) { continue; }

                var address = $"http://{node.Server}:{node.ClusterPort}";

                try
                {
                    var channel = GrpcChannel.ForAddress(address);
                    var client = new PlanarCluster.PlanarClusterClient(channel);
                    await client.HealthCheckAsync(new Empty());
                    await _dal.UpdateClusterHealthCheckDate(node); // TODO: make update more efficient
                }
                catch (RpcException ex)
                {
                    // TODO: make 3 times retry
                    _logger.LogError(ex, "Fail to make health check to {Address}", address);
                    await _dal.RemoveClusterNode(node);
                }
            }
        }

        public static async Task Schedule(IScheduler scheduler)
        {
            var jobKey = new JobKey(nameof(ClusterHealthCheckJob), Consts.PlanarSystemGroup);
            IJobDetail job = null;

            try
            {
                job = await scheduler.GetJobDetail(jobKey);
            }
            catch (Exception)
            {
                try
                {
                    await scheduler.DeleteJob(jobKey);
                }
                catch
                {
                    // *** DO NOTHING *** //
                }
                finally
                {
                    job = null;
                }
            }

            if (job != null) { return; }

            var jobId = ServiceUtil.GenerateId();
            var triggerId = ServiceUtil.GenerateId();

            job = JobBuilder.Create(typeof(ClusterHealthCheckJob))
                .WithIdentity(jobKey)
                .UsingJobData(Consts.JobId, jobId)
                .WithDescription("System job for check health of all cluster nodes")
                .StoreDurably(true)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity(jobKey.Name, jobKey.Group)
                .StartAt(new DateTimeOffset(DateTime.Now.Add(AppSettings.ClusterHealthCheckInterval)))
                .UsingJobData(Consts.TriggerId, triggerId)
                .WithSimpleSchedule(s => s
                    .WithInterval(AppSettings.ClusterHealthCheckInterval)
                    .RepeatForever()
                    .WithMisfireHandlingInstructionIgnoreMisfires()
                )
                .Build();

            await scheduler.ScheduleJob(job, trigger);

            if (AppSettings.Clustering == false)
            {
                await scheduler.PauseJob(jobKey);
            }
        }
    }
}