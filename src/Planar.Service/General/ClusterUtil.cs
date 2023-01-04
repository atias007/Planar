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
    public class ClusterUtil
    {
        private readonly ClusterData _dal;
        private readonly ILogger<ClusterUtil> _logger;
        private readonly SchedulerUtil _schedulerUtil;

        public ClusterUtil(ClusterData dal, ILogger<ClusterUtil> logger, SchedulerUtil schedulerUtil)
        {
            _dal = dal;
            _logger = logger;
            _schedulerUtil = schedulerUtil;
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

        private static DateTime GrpcDeadLine
        {
            get
            {
                return DateTime.UtcNow.AddSeconds(2);
            }
        }

        public async Task<IEnumerable<ClusterNode>> GetAllNodes()
        {
            var tempNode = GetCurrentClusterNode();
            var result = await _dal.GetClusterNodes();
            var currentNode = result.FirstOrDefault(n => string.Equals(n.Server, tempNode.Server, StringComparison.CurrentCultureIgnoreCase) && n.Port == tempNode.Port);
            if (currentNode != null)
            {
                currentNode.IsCurrentNode = true;
            }

            return result;
        }

        public async Task ValidateJobFolderExists(string folder)
        {
            var nodes = await GetAllNodes();
            foreach (var node in nodes)
            {
                if (node.IsCurrentNode) { continue; }

                try
                {
                    var result = await Policy.Handle<RpcException>()
                        .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(100))
                        .ExecuteAsync(() => CallIsJobFolderExistsService(node, folder));

                    if (!result.Exists)
                    {
                        throw new PlanarException($"folder {result.Path} is not exists. (node {Environment.MachineName})");
                    }
                }
                catch (RpcException ex)
                {
                    _logger.LogError(ex, "Fail to validate job folder exists at remote cluster node {Server}:{Port}", node.Server, node.ClusterPort);
                }
            }
        }

        public async Task ValidateJobFileExists(string folder, string filename)
        {
            var nodes = await GetAllNodes();
            foreach (var node in nodes)
            {
                if (node.IsCurrentNode) { continue; }

                try
                {
                    var result = await Policy.Handle<RpcException>()
                        .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(100))
                        .ExecuteAsync(() => CallIsJobFileExistsService(node, folder, filename));

                    if (!result.Exists)
                    {
                        throw new PlanarException($"folder {result.Path} does not have {filename} filename. (node {Environment.MachineName})");
                    }
                }
                catch (RpcException ex)
                {
                    _logger.LogError(ex, "Fail to validate job folder exists at remote cluster node {Server}:{Port}", node.Server, node.ClusterPort);
                }
            }
        }

        public async Task HealthCheckWithUpdate()
        {
            var nodes = await GetAllNodes();
            await RegisterCurrentNodeIfNotExists(nodes);

            foreach (var node in nodes)
            {
                if (node.IsCurrentNode)
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

                    if (!node.LiveNode)
                    {
                        _logger.LogError("remove node {Server}:{Port} from cluster due to health check failure", node.Server, node.ClusterPort);
                        await _dal.RemoveClusterNode(node);
                    }
                }
            }
        }

        public static void ValidateClusterConflict(IEnumerable<ClusterNode> nodes)
        {
            // only if current node is not clustering
            // and there is active cluster in same database --> throw exception
            if (AppSettings.Clustering) { return; }

            foreach (var node in nodes)
            {
                if (node.IsCurrentNode) { continue; }
                if (node.LiveNode)
                {
                    throw new PlanarException("start up node fail. node {Server}:{Port} is not clustering but connected database contains live cluster nodes");
                }
            }
        }

        public async Task<bool> HealthCheck()
        {
            var nodes = await GetAllNodes();
            return await HealthCheck(nodes);
        }

        public async Task<bool> HealthCheck(IEnumerable<ClusterNode> nodes)
        {
            var result = true;

            foreach (var node in nodes)
            {
                if (node.IsCurrentNode) { continue; }

                try
                {
                    await Policy.Handle<RpcException>()
                      .WaitAndRetryAsync(6, i => TimeSpan.FromMilliseconds(100))
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
                currentNode.InstanceId = _schedulerUtil.SchedulerInstanceId;
                await _dal.AddClusterNode(currentNode);
            }
            else
            {
                item.ClusterPort = AppSettings.ClusterPort;
                item.JoinDate = DateTime.Now;
                item.HealthCheckDate = DateTime.Now;
                item.InstanceId = _schedulerUtil.SchedulerInstanceId;
                await _dal.SaveChangesAsync();
            }
        }

        public async Task StopScheduler()
        {
            var nodes = await GetAllNodes();
            foreach (var node in nodes)
            {
                if (node.IsCurrentNode) { continue; }

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
            var nodes = await GetAllNodes();
            foreach (var node in nodes)
            {
                if (node.IsCurrentNode) { continue; }

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

            var nodes = await GetAllNodes();
            foreach (var node in nodes)
            {
                if (node.IsCurrentNode) { continue; }

                try
                {
                    var result = await Policy.Handle<RpcException>()
                    .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(100))
                    .ExecuteAsync(() => CallIsJobRunningService(rcpJobKey, node));

                    if (result) { return result; }
                }
                catch (RpcException ex)
                {
                    _logger.LogError(ex, "Fail to check if job is running at remote cluster node {Server}:{Port}", node.Server, node.ClusterPort);
                }
            }

            return false;
        }

        public async Task<RunningJobDetails> GetRunningJob(string instanceId)
        {
            var nodes = await GetAllNodes();
            foreach (var node in nodes)
            {
                if (node.IsCurrentNode) { continue; }

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

        public async Task<GetRunningDataResponse> GetRunningData(string instanceId)
        {
            var nodes = await GetAllNodes();
            foreach (var node in nodes)
            {
                if (node.IsCurrentNode) { continue; }

                try
                {
                    var job = await Policy.Handle<RpcException>()
                        .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(100))
                        .ExecuteAsync(() => CallGetRunningData(node, instanceId));

                    if (job != null)
                    {
                        return job;
                    }
                }
                catch (RpcException ex)
                {
                    _logger.LogError(ex, "Fail to get running job {InstanceId} data at remote cluster node {Server}:{Port}", instanceId, node.Server, node.ClusterPort);
                }
            }

            return null;
        }

        public async Task<bool> IsRunningInstanceExist(string instanceId)
        {
            var nodes = await GetAllNodes();
            foreach (var node in nodes)
            {
                if (node.IsCurrentNode) { continue; }

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

        public async Task<bool> StopRunningJob(string instanceId)
        {
            var nodes = await GetAllNodes();
            foreach (var node in nodes)
            {
                if (node.IsCurrentNode) { continue; }

                try
                {
                    var isStopped = await Policy.Handle<RpcException>()
                        .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(100))
                        .ExecuteAsync(() => CallStopRunningJob(node, instanceId));

                    if (isStopped) { return true; }
                }
                catch (RpcException ex)
                {
                    _logger.LogError(ex, "Fail to stop running instance {InstanceId} at remote cluster node {Server}:{Port}", instanceId, node.Server, node.ClusterPort);
                }
            }

            return false;
        }

        public async Task<List<RunningJobDetails>> GetRunningJobs()
        {
            var result = new List<RunningJobDetails>();
            var nodes = await GetAllNodes();
            foreach (var node in nodes)
            {
                if (node.IsCurrentNode) { continue; }

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
            var nodes = await GetAllNodes();
            foreach (var node in nodes)
            {
                if (node.IsCurrentNode) { continue; }

                try
                {
                    var items = await Policy.Handle<RpcException>()
                        .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(100))
                        .ExecuteAsync(() => CallGetPersistanceRunningJobInfo(node));

                    var mapItems = items.RunningJobs.Select(i => PersistanceRunningJobsInfo.Parse(i));
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
            const string schema = "http";
            var address = $"{schema}://{node.Server}:{node.ClusterPort}";
            var channel = GrpcChannel.ForAddress(address);
            var client = new PlanarCluster.PlanarClusterClient(channel);
            return client;
        }

        private async Task CallHealthCheckService(ClusterNode node)
        {
            var client = GetClient(node);
            await client.HealthCheckAsync(new Empty(), deadline: GrpcDeadLine);

            node.HealthCheckDate = DateTime.Now;
            await _dal.SaveChangesAsync();
        }

        private static async Task CallStopSchedulerService(ClusterNode node)
        {
            var client = GetClient(node); ;
            await client.StopSchedulerAsync(new Empty(), deadline: GrpcDeadLine);
        }

        private static async Task<IsJobAssestsExistReply> CallIsJobFolderExistsService(ClusterNode node, string folder)
        {
            var client = GetClient(node); ;
            var request = new IsJobAssestsExistRequest { Folder = folder };
            var result = await client.IsJobFolderExistAsync(request, deadline: GrpcDeadLine);
            return result;
        }

        private static async Task<IsJobAssestsExistReply> CallIsJobFileExistsService(ClusterNode node, string folder, string filename)
        {
            var client = GetClient(node); ;
            var request = new IsJobAssestsExistRequest { Folder = folder, Filename = filename };
            var result = await client.IsJobFileExistAsync(request, deadline: GrpcDeadLine);
            return result;
        }

        private static async Task CallStartSchedulerService(ClusterNode node)
        {
            var client = GetClient(node);
            await client.StartSchedulerAsync(new Empty(), deadline: GrpcDeadLine);
        }

        private static async Task<bool> CallIsJobRunningService(RpcJobKey jobKey, ClusterNode node)
        {
            var client = GetClient(node);
            var result = await client.IsJobRunningAsync(jobKey, deadline: GrpcDeadLine);
            return result.IsRunning;
        }

        private static async Task<List<RunningJobDetails>> CallGetRunningJobs(ClusterNode node)
        {
            var client = GetClient(node);
            var result = await client.GetRunningJobsAsync(new Empty(), deadline: GrpcDeadLine);

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
            var result = await client.GetRunningJobAsync(request, deadline: GrpcDeadLine);

            if (result.IsEmpty)
            {
                return null;
            }

            var response = MapRunningJob(result);
            return response;
        }

        private static async Task<GetRunningDataResponse> CallGetRunningData(ClusterNode node, string instanceId)
        {
            var client = GetClient(node);
            var request = new GetRunningJobRequest { InstanceId = instanceId };
            var result = await client.GetRunningDataAsync(request, deadline: GrpcDeadLine);

            if (result.IsEmpty)
            {
                return null;
            }

            var response = new GetRunningDataResponse
            {
                Log = string.IsNullOrEmpty(result.Log) ? null : result.Log,
                Exceptions = string.IsNullOrEmpty(result.Exceptions) ? null : result.Exceptions
            };

            return response;
        }

        private static async Task<bool> CallIsRunningInstanceExist(ClusterNode node, string instanceId)
        {
            var client = GetClient(node);
            var request = new GetRunningJobRequest { InstanceId = instanceId };
            var result = await client.IsRunningInstanceExistAsync(request, deadline: GrpcDeadLine);
            return result.Exists;
        }

        private static async Task<bool> CallStopRunningJob(ClusterNode node, string instanceId)
        {
            var client = GetClient(node);
            var request = new GetRunningJobRequest { InstanceId = instanceId };
            var result = await client.StopRunningJobAsync(request, deadline: GrpcDeadLine);
            return result.IsStopped;
        }

        private static async Task<PersistanceRunningJobInfoReply> CallGetPersistanceRunningJobInfo(ClusterNode node)
        {
            var client = GetClient(node);
            var result = await client.GetPersistanceRunningJobInfoAsync(new Empty(), deadline: GrpcDeadLine);
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
                RunTime = reply.RunTime.ToTimeSpan(),
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

            var result = stamp.ToDateTimeOffset().DateTime;
            if (result == default) { return null; }

            return result;
        }

        private static DateTime ConvertTimeStamp2(Timestamp stamp)
        {
            if (stamp == null) { return default; }

            var result = stamp.ToDateTimeOffset().DateTime;

            return result;
        }

        private async Task RegisterCurrentNodeIfNotExists(IEnumerable<ClusterNode> allNodes)
        {
            if (!allNodes.Any(n => n.IsCurrentNode) && _schedulerUtil.IsSchedulerRunning)
            {
                var currentNode = new ClusterNode
                {
                    ClusterPort = AppSettings.ClusterPort,
                    JoinDate = DateTime.Now,
                    InstanceId = _schedulerUtil.SchedulerInstanceId,
                    HealthCheckDate = DateTime.Now,
                };

                await _dal.AddClusterNode(currentNode);
            }
        }

        private async Task VerifyCurrentNode(ClusterNode node)
        {
            if (node.InstanceId != _schedulerUtil.SchedulerInstanceId)
            {
                node.InstanceId = _schedulerUtil.SchedulerInstanceId;
                node.JoinDate = DateTime.Now;
            }

            if (node.ClusterPort != AppSettings.ClusterPort)
            {
                node.ClusterPort = AppSettings.ClusterPort;
            }

            node.HealthCheckDate = DateTime.Now;

            await _dal.SaveChangesAsync();
        }
    }
}