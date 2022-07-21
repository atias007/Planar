using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.Data;
using Planar.Service.General;
using Planar.Service.Model;
using Polly;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
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
            };

            var nodes = await _dal.GetClusterNodes();
            await RegisterCurrentNodeIfNotExists(currentNode, nodes);

            foreach (var node in nodes)
            {
                if (node == currentNode)
                {
                    await VerifyCurrentNode(node);
                    continue;
                }

                try
                {
                    await Policy.Handle<RpcException>()
                    .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(500))
                    .ExecuteAsync(() => CallHealthCheckService(node));
                }
                catch (RpcException ex)
                {
                    _logger.LogError(ex, "Fail to make health check to {Server}:{Port}", node.Server, node.ClusterPort);
                    await _dal.RemoveClusterNode(node);
                }
            }
        }

        private async Task CallHealthCheckService(ClusterNode node)
        {
            var address = $"http://{node.Server}:{node.ClusterPort}";
            var channel = GrpcChannel.ForAddress(address);
            var client = new PlanarCluster.PlanarClusterClient(channel);
            await client.HealthCheckAsync(new Empty());

            node.HealthCheckDate = DateTime.Now;
            await _dal.SaveChanges();
        }

        private async Task RegisterCurrentNodeIfNotExists(ClusterNode currentNode, List<ClusterNode> allNodes)
        {
            if (allNodes.Any(n => n == currentNode) == false)
            {
                currentNode.ClusterPort = AppSettings.ClusterPort;
                currentNode.JoinDate = DateTime.Now;
                currentNode.InstanceId = MainService.Scheduler.SchedulerInstanceId;
                currentNode.HealthCheckDate = DateTime.Now;

                await _dal.AddClusterNode(currentNode);
            }
        }

        private async Task VerifyCurrentNode(ClusterNode node)
        {
            if (node.InstanceId != MainService.Scheduler.SchedulerInstanceId)
            {
                node.InstanceId = MainService.Scheduler.SchedulerInstanceId;
                node.JoinDate = DateTime.Now;
            }

            if (node.ClusterPort != AppSettings.ClusterPort)
            {
                node.ClusterPort = AppSettings.ClusterPort;
            }

            node.HealthCheckDate = DateTime.Now;

            await _dal.SaveChanges();
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

            if (job != null)
            {
                await scheduler.DeleteJob(jobKey);
                job = await scheduler.GetJobDetail(jobKey);

                if (job != null) { return; }
            }

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
                    .WithMisfireHandlingInstructionNextWithExistingCount()
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