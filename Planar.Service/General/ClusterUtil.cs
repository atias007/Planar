using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Planar.Service.Data;
using Planar.Service.Model;
using Polly;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.General
{
    internal class ClusterUtil
    {
        private readonly DataLayer _dal;
        private readonly ILogger _logger;

        public ClusterUtil(DataLayer dal, ILogger logger)
        {
            _dal = dal;
            _logger = logger;
        }

        private static ClusterNode GetCurrentClusterNode()
        {
            var cluster = new ClusterNode
            {
                Server = Environment.MachineName,
                Port = AppSettings.HttpPort,
            };

            return cluster;
        }

        public async Task HealthCheckWithUpdate()
        {
            var currentNode = GetCurrentClusterNode();

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

        public async Task<bool> HealthCheck()
        {
            var currentNode = GetCurrentClusterNode();
            var result = true;

            var nodes = await _dal.GetClusterNodes();

            foreach (var node in nodes)
            {
                if (node == currentNode) { continue; }

                try
                {
                    await Policy.Handle<RpcException>()
                    .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(500))
                    .ExecuteAsync(() => CallHealthCheckService(node));
                }
                catch (RpcException ex)
                {
                    _logger.LogError(ex, "Fail to make health check to {Server}:{Port}", node.Server, node.ClusterPort);
                    result = false;
                }
            }

            return result;
        }

        public async Task Join()
        {
            var currentNode = GetCurrentClusterNode();

            var item = await _dal.GetClusterNode(currentNode);
            if (item == null)
            {
                currentNode.ClusterPort = AppSettings.ClusterPort;
                currentNode.JoinDate = DateTime.Now;
                currentNode.InstanceId = MainService.Scheduler.SchedulerInstanceId;
                await _dal.AddClusterNode(currentNode);
            }
            else
            {
                item.ClusterPort = AppSettings.ClusterPort;
                item.JoinDate = DateTime.Now;
                item.HealthCheckDate = null;
                item.InstanceId = MainService.Scheduler.SchedulerInstanceId;
                await _dal.SaveChanges();
            }
        }

        public async Task StopScheduler()
        {
            var currentNode = GetCurrentClusterNode();

            var nodes = await _dal.GetClusterNodes();
            foreach (var node in nodes)
            {
                if (node == currentNode) { continue; }

                try
                {
                    await Policy.Handle<RpcException>()
                    .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(500))
                    .ExecuteAsync(() => CallStopSchedulerService(node));
                }
                catch (RpcException ex)
                {
                    _logger.LogError(ex, "Fail to stop scheduler at remote cluster node {Server}:{Port}", node.Server, node.ClusterPort);
                }
            }
        }

        public async Task StartScheduler()
        {
            var currentNode = GetCurrentClusterNode();

            var nodes = await _dal.GetClusterNodes();
            foreach (var node in nodes)
            {
                if (node == currentNode) { continue; }

                try
                {
                    await Policy.Handle<RpcException>()
                    .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(500))
                    .ExecuteAsync(() => CallStartSchedulerService(node));
                }
                catch (RpcException ex)
                {
                    _logger.LogError(ex, "Fail to start scheduler at remote cluster node {Server}:{Port}", node.Server, node.ClusterPort);
                }
            }
        }

        public async Task<bool> IsJobRunning(JobKey jobKey)
        {
            var rcpJobKey = new RpcJobKey { Group = jobKey.Group, Name = jobKey.Name };

            var currentNode = GetCurrentClusterNode();

            var nodes = await _dal.GetClusterNodes();
            foreach (var node in nodes)
            {
                if (node == currentNode) { continue; }

                try
                {
                    var result = await Policy.Handle<RpcException>()
                    .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(500))
                    .ExecuteAsync(() => CallIsJobRunningService(rcpJobKey, node));

                    if (result) { return result; }
                }
                catch (RpcException ex)
                {
                    _logger.LogError(ex, "Fail to start scheduler at remote cluster node {Server}:{Port}", node.Server, node.ClusterPort);
                }
            }

            return false;
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

        private static async Task CallStopSchedulerService(ClusterNode node)
        {
            var address = $"http://{node.Server}:{node.ClusterPort}";
            var channel = GrpcChannel.ForAddress(address);
            var client = new PlanarCluster.PlanarClusterClient(channel);
            await client.StopSchedulerAsync(new Empty());
        }

        private static async Task CallStartSchedulerService(ClusterNode node)
        {
            var address = $"http://{node.Server}:{node.ClusterPort}";
            var channel = GrpcChannel.ForAddress(address);
            var client = new PlanarCluster.PlanarClusterClient(channel);
            await client.StartSchedulerAsync(new Empty());
        }

        private static async Task<bool> CallIsJobRunningService(RpcJobKey jobKey, ClusterNode node)
        {
            var address = $"http://{node.Server}:{node.ClusterPort}";
            var channel = GrpcChannel.ForAddress(address);
            var client = new PlanarCluster.PlanarClusterClient(channel);
            var result = await client.IsJobRunningAsync(jobKey);
            return result.IsRunning;
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
    }
}