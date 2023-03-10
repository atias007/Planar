using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Common.Exceptions;
using Planar.Service.API;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Planar.Service.Model;
using Planar.Service.Monitor;
using Planar.Service.SystemJobs;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service
{
    public class MainService : BackgroundService
    {
        private readonly ILogger<MainService> _logger;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly IServiceProvider _serviceProvider;
        private readonly SchedulerUtil _schedulerUtil;

        public MainService(IServiceProvider serviceProvider, IHostApplicationLifetime lifetime)
        {
            _serviceProvider = serviceProvider;
            _lifetime = lifetime;
            _logger = _serviceProvider.GetRequiredService<ILogger<MainService>>();
            _schedulerUtil = _serviceProvider.GetRequiredService<SchedulerUtil>();
        }

        private static async Task<bool> WaitForAppStartup(IHostApplicationLifetime lifetime, CancellationToken stoppingToken)
        {
            var startedSource = new TaskCompletionSource();
            var cancelledSource = new TaskCompletionSource();

            using var reg1 = lifetime.ApplicationStarted.Register(() => startedSource.SetResult());
            using var reg2 = stoppingToken.Register(() => cancelledSource.SetResult());

            Task completedTask = await Task.WhenAny(
                startedSource.Task,
                cancelledSource.Task).ConfigureAwait(false);

            // If the completed tasks was the "app started" task, return true, otherwise false
            return completedTask == startedSource.Task;
        }

        public void Shutdown()
        {
            var removeTask = RemoveSchedulerCluster();
            if (_serviceProvider.GetService(typeof(IHostApplicationLifetime)) is IHostApplicationLifetime app)
            {
                try
                {
                    removeTask.Wait(3000);
                }
                catch
                {
                    // *** IGNORE EXCEPTION *** //
                }
                app.StopApplication();
            }
            Global.Clear();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!await WaitForAppStartup(_lifetime, stoppingToken))
            {
                return;
            }

            _ = Run(stoppingToken)
                .ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        _logger.LogError(t.Exception, "Unhandled exception: {Message}", t.Exception.Message);
                    }
                }, stoppingToken);

            var waiter = new CancellationTokenAwaiter(stoppingToken);

            waiter.OnCompleted(async () =>
            {
                _logger.LogInformation("IsCancellationRequested = {Value}", stoppingToken.IsCancellationRequested);
                try
                {
                    RemoveSchedulerCluster().Wait();
                }
                catch
                {
                    _logger.LogWarning("Fail to {Operation}", nameof(RemoveSchedulerCluster));
                }

                try
                {
                    await _schedulerUtil.Shutdown(stoppingToken);
                }
                catch
                {
                    // *** ignore exceptions *** //
                }
            });

            await Task.CompletedTask;
        }

        public async Task Run(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Service environment: {Environment}", AppSettings.Environment);

            await LoadGlobalConfigInner(stoppingToken);

            await LoadMonitorHooks();

            await ScheduleSystemJobs(stoppingToken);

            await JoinToCluster(stoppingToken);
        }

        #region Initialize Scheduler

        private async Task LoadGlobalConfigInner(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Initialize: {Operation}", nameof(LoadGlobalConfig));
                await LoadGlobalConfig(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Initialize: Fail to {Operation}", nameof(LoadGlobalConfig));
                Shutdown();
            }
        }

        public async Task LoadGlobalConfig(CancellationToken stoppingToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var config = scope.ServiceProvider.GetRequiredService<ConfigDomain>();
            await config.Flush(stoppingToken);
        }

        public async Task ScheduleSystemJobs(CancellationToken stoppingToken = default)
        {
            try
            {
                _logger.LogInformation("Initialize: {Operation}", nameof(ScheduleSystemJobs));
                await PersistDataJob.Schedule(_schedulerUtil.Scheduler, stoppingToken);
                await ClusterHealthCheckJob.Schedule(_schedulerUtil.Scheduler, stoppingToken);
                await ClearHistoryJob.Schedule(_schedulerUtil.Scheduler, stoppingToken);
                await StatisticsJob.Schedule(_schedulerUtil.Scheduler, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Initialize: Fail to {Operation}", nameof(ScheduleSystemJobs));
                Shutdown();
            }
        }

        private async Task LoadMonitorHooks()
        {
            try
            {
                _logger.LogInformation("Initialize: {Operation}", nameof(LoadMonitorHooks));

                ServiceUtil.LoadMonitorHooks(_logger);
                using var scope = _serviceProvider.CreateScope();
                var monitor = scope.ServiceProvider.GetRequiredService<MonitorUtil>();
                await monitor.Validate();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Initialize: Fail to {Operation}", nameof(LoadMonitorHooks));
            }
        }

        private async Task RemoveSchedulerCluster()
        {
            if (!AppSettings.Clustering) { return; }

            var cluster = new ClusterNode
            {
                Server = Environment.MachineName,
                Port = AppSettings.HttpPort,
                InstanceId = _schedulerUtil.SchedulerInstanceId
            };

            var services = new ServiceCollection();
            services.AddPlanarDataLayerWithContext();
            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var dal = scope.ServiceProvider.GetRequiredService<ClusterData>();
            await dal.RemoveClusterNode(cluster);

            var info = new MonitorSystemInfo("Cluster node removed from {{MachineName}}");
            info.MessagesParameters.Add("Port", AppSettings.HttpPort.ToString());
            info.MessagesParameters.Add("InstanceId", _schedulerUtil.SchedulerInstanceId);
            info.AddMachineName();
            await SafeSystemScan(MonitorEvents.ClusterNodeRemoved, info);
        }

        private async Task JoinToCluster(CancellationToken stoppingToken)
        {
            if (!AppSettings.Clustering) { return; }

            try
            {
                _logger.LogInformation("Initialize: {Operation}", nameof(JoinToCluster));

                using var scope = _serviceProvider.CreateScope();
                var util = scope.ServiceProvider.GetRequiredService<ClusterUtil>();
                var nodes = await util.GetAllNodes();
                ClusterUtil.ValidateClusterConflict(nodes);

                var liveNodes = nodes.Where(n => n.LiveNode).ToList();
                var deadNodes = nodes.Where(n => !n.LiveNode).ToList();
                LogDeadNodes(deadNodes);

                if (await util.HealthCheck(liveNodes))
                {
                    await util.Join();
                    LogClustering();

                    var info = new MonitorSystemInfo("Cluster node join to {{MachineName}}");
                    info.MessagesParameters.Add("Port", AppSettings.HttpPort.ToString());
                    info.MessagesParameters.Add("InstanceId", _schedulerUtil.SchedulerInstanceId);
                    info.AddMachineName();
                    await SafeSystemScan(MonitorEvents.ClusterNodeJoin, info, cancellationToken: stoppingToken);
                }
                else
                {
                    await _schedulerUtil.Stop(stoppingToken);
                    throw new PlanarException("cluster health check fail. Could not join to cluster. See previous errors for more details");
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Initialize: Fail to {Operation}", nameof(JoinToCluster));
                await _schedulerUtil.Stop(stoppingToken);
                await _schedulerUtil.Shutdown(stoppingToken);
                Shutdown();
            }
        }

        private void LogDeadNodes(List<ClusterNode> nodes)
        {
            if (nodes != null && nodes.Any())
            {
                var text = string.Join(',', nodes.Select(n => $"{n.Server}:{n.Port}").ToArray());
                _logger.LogWarning("There are dead cluster nodes. Current node will not check health with them {Nodes}", text);
            }
        }

        private void LogClustering()
        {
            if (AppSettings.Clustering)
            {
                _logger.LogInformation("Join to cluster [instance id: {Id}]", _schedulerUtil.SchedulerInstanceId);
            }
            else
            {
                _logger.LogInformation("Non clustering instance");
            }
        }

        #endregion Initialize Scheduler

        protected async Task SafeSystemScan(MonitorEvents @event, MonitorSystemInfo info, Exception exception = default, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!MonitorEventsExtensions.IsSystemMonitorEvent(@event)) { return; }

                using var scope = _serviceProvider.CreateScope();
                var monitor = scope.ServiceProvider.GetRequiredService<MonitorUtil>();
                await monitor.Scan(@event, info, exception);
            }
            catch (Exception ex)
            {
                var source = nameof(SafeSystemScan);
                _logger.LogCritical(ex, "Error handle {Source}: {Message} ", source, ex.Message);
            }
        }
    }
}