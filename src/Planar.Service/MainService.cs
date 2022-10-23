using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Planar.Common;
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
        private static IScheduler _scheduler;
        private readonly ILogger<MainService> _logger;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly IServiceProvider _serviceProvider;

        public MainService(IServiceProvider serviceProvider, IHostApplicationLifetime lifetime)
        {
            _serviceProvider = serviceProvider;
            _lifetime = lifetime;
            _logger = _serviceProvider.GetService<ILogger<MainService>>();
        }

        public static IScheduler Scheduler
        {
            get
            {
                if (_scheduler == null)
                {
                    throw new ApplicationException("Scheduler is not initialized");
                }

                return _scheduler;
            }
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

            _ = Run(stoppingToken);
            var waiter = new CancellationTokenAwaiter(stoppingToken);

            waiter.OnCompleted(async () =>
            {
                _logger.LogInformation("IsCancellationRequested = true");
                try
                {
                    RemoveSchedulerCluster().Wait();
                }
                catch
                {
                    _logger.LogWarning("Fail to RemoveSchedulerCluster");
                }

                try
                {
                    await _scheduler?.Shutdown(true);
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
            _logger.LogInformation("Service environment: {Environment}", Global.Environment);

            await LoadGlobalConfigInner(stoppingToken);

            await InitializeScheduler(_logger, _serviceProvider, stoppingToken);

            await LoadMonitorHooks();

            await ScheduleSystemJobs(stoppingToken);

            await JoinToCluster(stoppingToken);
        }

        #region Initialize Scheduler

        private async Task LoadGlobalConfigInner(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Initialize: LoadGlobalConfig");
                await LoadGlobalConfig(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Initialize: Fail to LoadGlobalConfig");
                throw;
            }
        }

        private static async Task InitializeScheduler(ILogger<MainService> logger, IServiceProvider serviceProvider, CancellationToken stoppingToken)
        {
            try
            {
                logger.LogInformation("Initialize: InitializeScheduler");
                _scheduler = await serviceProvider.GetRequiredService<ISchedulerFactory>().GetScheduler(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Initialize: Fail to InitializeScheduler");
                throw;
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
                _logger.LogInformation("Initialize: ScheduleSystemJobs");
                await PersistDataJob.Schedule(Scheduler, stoppingToken);
                await ClusterHealthCheckJob.Schedule(Scheduler, stoppingToken);
                await ClearTraceTableJob.Schedule(Scheduler, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Initialize: Fail to ScheduleSystemJobs");
                throw;
            }
        }

        private async Task LoadMonitorHooks()
        {
            try
            {
                _logger.LogInformation("Initialize: LoadMonitorHooks");

                ServiceUtil.LoadMonitorHooks(_logger);
                using var scope = _serviceProvider.CreateScope();
                var monitor = scope.ServiceProvider.GetRequiredService<MonitorUtil>();
                await monitor.Validate();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Initialize: Fail to AddMonitorHooks");
                throw;
            }
        }

        private static async Task RemoveSchedulerCluster()
        {
            if (AppSettings.Clustering)
            {
                var cluster = new ClusterNode
                {
                    Server = Environment.MachineName,
                    Port = AppSettings.HttpPort,
                    InstanceId = _scheduler.SchedulerInstanceId
                };

                var services = new ServiceCollection();
                services.AddPlanarDataLayerWithContext();
                var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dal = scope.ServiceProvider.GetRequiredService<DataLayer>();
                await dal.RemoveClusterNode(cluster);
            }
        }

        private async Task JoinToCluster(CancellationToken stoppingToken)
        {
            if (!AppSettings.Clustering) { return; }

            try
            {
                _logger.LogInformation("Initialize: JoinToCluster");

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
                }
                else
                {
                    Scheduler?.Standby(stoppingToken);
                    throw new PlanarException("Cluster health check fail. Could not join to cluster. See previous errors for more details");
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Initialize: Fail to AddSchedulerCluster");
                await _scheduler.Standby(stoppingToken);
                await _scheduler.Shutdown(stoppingToken);
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
                _logger.LogInformation("Join to cluster [instance id: {Id}]", _scheduler.SchedulerInstanceId);
            }
            else
            {
                _logger.LogInformation("Non clustering instance");
            }
        }

        #endregion Initialize Scheduler
    }
}