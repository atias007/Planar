using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.Model;
using Planar.Service.Model.DataObjects;
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
                      .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(100))
                      .ExecuteAsync(() => CallHealthCheckService(node));
                }
                catch (RpcException ex)
                {
                    _logger.LogError(ex, "fail to make health check to {Server}:{Port}", node.Server, node.ClusterPort);

                    if (node.LiveNode == false)
                    {
                        _logger.LogError("remove node {Server}:{Port} from cluster due to health check failure", node.Server, node.ClusterPort);
                        await _dal.RemoveClusterNode(node);
                    }
                }
            }
        }

        public static void ValidateClusterConflict(List<ClusterNode> nodes)
        {
            // only if current node is not clustering
            // and there is active cluster in same database --> throw exception
            if (AppSettings.Clustering) { return; }

            var currentNode = GetCurrentClusterNode();
            foreach (var node in nodes)
            {
                if (node == currentNode) { continue; }
                if (node.LiveNode)
                {
                    throw new PlanarException("start up node fail. node {Server}:{Port} is not clustering but connected database contains live cluster nodes");
                }
            }
        }

        public async Task<bool> HealthCheck(List<ClusterNode> nodes)
        {
            var currentNode = GetCurrentClusterNode();
            var result = true;

            foreach (var node in nodes)
            {
                if (node == currentNode) { continue; }

                try
                {
                    await Policy.Handle<RpcException>()
                      .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(100))
                      .ExecuteAsync(() => CallHealthCheckService(node));
                }
                catch (RpcException ex)
                {
                    _logger.LogError(ex, "fail to make health check to {Server}:{Port}", node.Server, node.ClusterPort);
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
                currentNode.HealthCheckDate = DateTime.Now;
                currentNode.InstanceId = MainService.Scheduler.SchedulerInstanceId;
                await _dal.AddClusterNode(currentNode);
            }
            else
            {
                item.ClusterPort = AppSettings.ClusterPort;
                item.JoinDate = DateTime.Now;
                item.HealthCheckDate = DateTime.Now;
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
                    .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(100))
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
                    .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(100))
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
                    .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(100))
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

        public async Task<RunningJobDetails> GetRunningJob(string instanceId)
        {
            var currentNode = GetCurrentClusterNode();
            var nodes = await _dal.GetClusterNodes();
            foreach (var node in nodes)
            {
                if (node == currentNode) { continue; }

                try
                {
                    var job = await Policy.Handle<RpcException>()
                        .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(100))
                        .ExecuteAsync(() => CallGetRunningJob(node, instanceId));

                    if (job != null)
                    {
                        return job;
                    }
                }
                catch (RpcException ex)
                {
                    _logger.LogError(ex, "Fail to get running job {InstanceId} at remote cluster node {Server}:{Port}", instanceId, node.Server, node.ClusterPort);
                }
            }

            return null;
        }

        public async Task<GetRunningInfoResponse> GetRunningInfo(string instanceId)
        {
            var currentNode = GetCurrentClusterNode();
            var nodes = await _dal.GetClusterNodes();
            foreach (var node in nodes)
            {
                if (node == currentNode) { continue; }

                try
                {
                    var job = await Policy.Handle<RpcException>()
                        .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(100))
                        .ExecuteAsync(() => CallGetRunningInfo(node, instanceId));

                    if (job != null)
                    {
                        return job;
                    }
                }
                catch (RpcException ex)
                {
                    _logger.LogError(ex, "Fail to get running job {InstanceId} information at remote cluster node {Server}:{Port}", instanceId, node.Server, node.ClusterPort);
                }
            }

            return null;
        }

        public async Task<bool> IsRunningInstanceExist(string instanceId)
        {
            var currentNode = GetCurrentClusterNode();
            var nodes = await _dal.GetClusterNodes();
            foreach (var node in nodes)
            {
                if (node == currentNode) { continue; }

                try
                {
                    var exists = await Policy.Handle<RpcException>()
                        .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(100))
                        .ExecuteAsync(() => CallIsRunningInstanceExist(node, instanceId));

                    if (exists) { return true; }
                }
                catch (RpcException ex)
                {
                    _logger.LogError(ex, "Fail to check is job {InstanceId} running at remote cluster node {Server}:{Port}", instanceId, node.Server, node.ClusterPort);
                }
            }

            return false;
        }

        public async Task StopRunningJob(string instanceId)
        {
            var currentNode = GetCurrentClusterNode();
            var nodes = await _dal.GetClusterNodes();
            foreach (var node in nodes)
            {
                if (node == currentNode) { continue; }

                try
                {
                    await Policy.Handle<RpcException>()
                        .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(100))
                        .ExecuteAsync(() => CallStopRunningJob(node, instanceId));
                }
                catch (RpcException ex)
                {
                    _logger.LogError(ex, "Fail to stop running instance {InstanceId} at remote cluster node {Server}:{Port}", instanceId, node.Server, node.ClusterPort);
                }
            }
        }

        public async Task<List<RunningJobDetails>> GetRunningJobs()
        {
            var result = new List<RunningJobDetails>();
            var currentNode = GetCurrentClusterNode();
            var nodes = await _dal.GetClusterNodes();
            foreach (var node in nodes)
            {
                if (node == currentNode) { continue; }

                try
                {
                    var jobs = await Policy.Handle<RpcException>()
                        .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(100))
                        .ExecuteAsync(() => CallGetRunningJobs(node));

                    result.AddRange(jobs);
                }
                catch (RpcException ex)
                {
                    _logger.LogError(ex, "Fail to get all running jobs at remote cluster node {Server}:{Port}", node.Server, node.ClusterPort);
                }
            }

            return result;
        }

        public async Task<List<PersistanceRunningJobsInfo>> GetPersistanceRunningJobsInfo()
        {
            var result = new List<PersistanceRunningJobsInfo>();
            var currentNode = GetCurrentClusterNode();
            var nodes = await _dal.GetClusterNodes();
            foreach (var node in nodes)
            {
                if (node == currentNode) { continue; }

                try
                {
                    var items = await Policy.Handle<RpcException>()
                        .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(100))
                        .ExecuteAsync(() => CallGetPersistanceRunningJobInfo(node));

                    var mapItems = items.RunningJobs.Select(i => new PersistanceRunningJobsInfo
                    {
                        Exceptions = i.Exceptions,
                        Group = i.Group,
                        Information = i.Information,
                        InstanceId = i.InstanceId,
                        Name = i.Name,
                    });

                    result.AddRange(mapItems);
                }
                catch (RpcException ex)
                {
                    _logger.LogError(ex, "Fail to get persistance running jobs info at remote cluster node {Server}:{Port}", node.Server, node.ClusterPort);
                }
            }

            return result;
        }

        private static PlanarCluster.PlanarClusterClient GetClient(ClusterNode node)
        {
            var address = $"http://{node.Server}:{node.ClusterPort}";
            var channel = GrpcChannel.ForAddress(address);
            var client = new PlanarCluster.PlanarClusterClient(channel);
            return client;
        }

        private async Task CallHealthCheckService(ClusterNode node)
        {
            var client = GetClient(node);
            await client.HealthCheckAsync(new Empty());

            node.HealthCheckDate = DateTime.Now;
            await _dal.SaveChanges();
        }

        private static async Task CallStopSchedulerService(ClusterNode node)
        {
            var client = GetClient(node); ;
            await client.StopSchedulerAsync(new Empty());
        }

        private static async Task CallStartSchedulerService(ClusterNode node)
        {
            var client = GetClient(node);
            await client.StartSchedulerAsync(new Empty());
        }

        private static async Task<bool> CallIsJobRunningService(RpcJobKey jobKey, ClusterNode node)
        {
            var client = GetClient(node);
            var result = await client.IsJobRunningAsync(jobKey);
            return result.IsRunning;
        }

        private static async Task<List<RunningJobDetails>> CallGetRunningJobs(ClusterNode node)
        {
            var client = GetClient(node);
            var result = await client.GetRunningJobsAsync(new Empty());

            var response = new List<RunningJobDetails>();
            foreach (var job in result.Jobs)
            {
                var entity = MapRunningJob(job);
                response.Add(entity);
            }

            return response;
        }

        private static async Task<RunningJobDetails> CallGetRunningJob(ClusterNode node, string instanceId)
        {
            var client = GetClient(node);
            var request = new GetRunningJobRequest { InstanceId = instanceId };
            var result = await client.GetRunningJobAsync(request);
            var response = MapRunningJob(result);
            return response;
        }

        private static async Task<GetRunningInfoResponse> CallGetRunningInfo(ClusterNode node, string instanceId)
        {
            var client = GetClient(node);
            var request = new GetRunningJobRequest { InstanceId = instanceId };
            var result = await client.GetRunningInfoAsync(request);
            var response = new GetRunningInfoResponse
            {
                Information = result.Information,
                Exceptions = result.Exceptions
            };

            return response;
        }

        private static async Task<bool> CallIsRunningInstanceExist(ClusterNode node, string instanceId)
        {
            var client = GetClient(node);
            var request = new GetRunningJobRequest { InstanceId = instanceId };
            var result = await client.IsRunningInstanceExistAsync(request);
            return result.Exists;
        }

        private static async Task CallStopRunningJob(ClusterNode node, string instanceId)
        {
            var client = GetClient(node);
            var request = new GetRunningJobRequest { InstanceId = instanceId };
            await client.StopRunningJobAsync(request);
        }

        private static async Task<PersistanceRunningJobInfoReply> CallGetPersistanceRunningJobInfo(ClusterNode node)
        {
            var client = GetClient(node);
            var result = await client.GetPersistanceRunningJobInfoAsync(new Empty());
            return result;
        }

        private static RunningJobDetails MapRunningJob(RunningJobReply reply)
        {
            var result = new RunningJobDetails
            {
                DataMap = new SortedDictionary<string, string>(reply.DataMap.ToDictionary(k => k.Key, v => v.Value)),
                Description = reply.Description,
                EffectedRows = reply.EffectedRows == -1 ? null : reply.EffectedRows,
                FireInstanceId = reply.FireInstanceId,
                FireTime = ConvertTimeStamp2(reply.FireTime),
                Group = reply.Group,
                Id = reply.Id,
                Name = reply.Name,
                NextFireTime = ConvertTimeStamp(reply.NextFireTime),
                PreviousFireTime = ConvertTimeStamp(reply.PreviousFireTime),
                Progress = reply.Progress,
                RefireCount = reply.RefireCount,
                RunTime = reply.RunTime,
                ScheduledFireTime = ConvertTimeStamp(reply.ScheduledFireTime),
                TriggerGroup = reply.TriggerGroup,
                TriggerId = reply.TriggerId,
                TriggerName = reply.TriggerName,
            };

            return result;
        }

        private static DateTime? ConvertTimeStamp(Timestamp stamp)
        {
            if (stamp == null) { return null; }
            if (stamp == default) { return null; }

            var result = stamp.ToDateTimeOffset().DateTime;
            if (result == default) { return null; }

            return result;
        }

        private static DateTime ConvertTimeStamp2(Timestamp stamp)
        {
            if (stamp == null) { return default; }
            if (stamp == default) { return default; }

            var result = stamp.ToDateTimeOffset().DateTime;

            return result;
        }

        private async Task RegisterCurrentNodeIfNotExists(ClusterNode currentNode, List<ClusterNode> allNodes)
        {
            if (allNodes.Any(n => n == currentNode) == false)
            {
                if (SchedulerUtil.IsSchedulerRunning)
                {
                    currentNode.ClusterPort = AppSettings.ClusterPort;
                    currentNode.JoinDate = DateTime.Now;
                    currentNode.InstanceId = MainService.Scheduler.SchedulerInstanceId;
                    currentNode.HealthCheckDate = DateTime.Now;

                    await _dal.AddClusterNode(currentNode);
                }
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