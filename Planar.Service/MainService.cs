using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Planar.Calendar.Hebrew;
using Planar.Common;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Planar.Service.List;
using Planar.Service.Model;
using Planar.Service.Monitor;
using Planar.Service.SystemJobs;
using Quartz;
using Quartz.Impl.Matchers;
using Quartz.Logging;
using Quartz.Simpl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Quartz.SchedulerBuilder;

namespace Planar.Service
{
    public class MainService : BackgroundService
    {
        private static IScheduler _scheduler;
        private readonly ILogger<MainService> _logger;
        private readonly IHostApplicationLifetime _lifetime;

        public MainService(IServiceProvider serviceProvider, IHostApplicationLifetime lifetime)
        {
            _lifetime = lifetime;
            _logger = serviceProvider.GetService<ILogger<MainService>>();
            Global.ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
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

        public static void Shutdown()
        {
            var removeTask = RemoveSchedulerCluster();
            if (Global.ServiceProvider.GetService(typeof(IHostApplicationLifetime)) is IHostApplicationLifetime app)
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

            var mainTask = Run();
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

        public async Task Run()
        {
            _logger.LogInformation("Service environment: {Environment}", Global.Environment);

            await LoadGlobalConfigInner();

            await SetQuartzLogProvider();

            await AddCalendarSerializer();

            await InitializeScheduler();

            await AddJobListeners();

            await AddCalendars();

            await LoadMonitorHooks();

            await ScheduleSystemJobs();

            await StartScheduler();

            await JoinToCluster();
        }

        #region Initialize Scheduler

        private async Task LoadGlobalConfigInner()
        {
            try
            {
                _logger.LogInformation("Initialize: LoadGlobalConfig");
                await LoadGlobalConfig();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Initialize: Fail to LoadGlobalConfig");
                throw;
            }
        }

        public static async Task LoadGlobalConfig()
        {
            var dal = Resolve<DataLayer>();
            var prms = await dal.GetAllGlobalConfig();
            var dict = prms.ToDictionary(p => p.Key, p => p.Value);
            Global.SetGlobalConfig(dict);
        }

        public async Task ScheduleSystemJobs()
        {
            try
            {
                _logger.LogInformation("Initialize: ScheduleSystemJobs");
                await PersistDataJob.Schedule(Scheduler);
                await ClusterHealthCheckJob.Schedule(Scheduler);
                await ClearTraceTableJob.Schedule(Scheduler);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Initialize: Fail to ScheduleSystemJobs");
                throw;
            }
        }

        private async Task SetQuartzLogProvider()
        {
            try
            {
                _logger.LogInformation("Initialize: SetQuartzLogProvider");
                var quartzLogger = Global.ServiceProvider.GetService<ILogger<QuartzLogProvider>>();
                LogProvider.SetCurrentLogProvider(new QuartzLogProvider(quartzLogger));
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Initialize: Fail to SetQuartzLogProvider");
                throw;
            }
        }

        private async Task StartScheduler()
        {
            try
            {
                _logger.LogInformation("Initialize: StartScheduler");

#if DEBUG
                var delaySeconds = 1;
#else
                var delaySeconds = 30;
#endif

                await _scheduler.StartDelayed(TimeSpan.FromSeconds(delaySeconds));
                _logger.LogInformation("Initialize: Scheduler is initializes and started :)) [with {Delay} seconds]", delaySeconds);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Initialize: Fail to StartScheduler");
                throw;
            }
        }

        private async Task AddJobListeners()
        {
            try
            {
                _logger.LogInformation("Initialize: AddJobListeners");
                _scheduler.ListenerManager.AddJobListener(new LogJobListener(), GroupMatcher<JobKey>.AnyGroup());
                _scheduler.ListenerManager.AddTriggerListener(new RetryTriggerListener(), GroupMatcher<TriggerKey>.AnyGroup());
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Initialize: Fail to AddJobListeners");
                throw;
            };
        }

        private async Task AddCalendars()
        {
            try
            {
                _logger.LogInformation("Initialize: AddCalendars");

                await _scheduler.AddCalendar(nameof(HebrewCalendar), new HebrewCalendar(_logger), true, true);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Initialize: Fail to AddCalendars");
                throw;
            };
        }

        private async Task LoadMonitorHooks()
        {
            try
            {
                _logger.LogInformation("Initialize: LoadMonitorHooks");

                ServiceUtil.LoadMonitorHooks(_logger);
                await MonitorUtil.Validate(_logger);
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
                var dal = provider.GetRequiredService<DataLayer>();
                await dal.RemoveClusterNode(cluster);
            }
        }

        private async Task JoinToCluster()
        {
            if (!AppSettings.Clustering) { return; }

            try
            {
                _logger.LogInformation("Initialize: JoinToCluster");

                var dal = Resolve<DataLayer>();
                var util = new ClusterUtil(dal, _logger);
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
                    Scheduler?.Standby();
                    throw new PlanarException("Cluster health check fail. Could not join to cluster. See previous errors for more details");
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Initialize: Fail to AddSchedulerCluster");
                await _scheduler.Standby();
                await _scheduler.Shutdown();
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

        private async Task InitializeScheduler()
        {
            try
            {
                _logger.LogInformation("Initialize: InitializeScheduler");

                var builder = Create()
                    .WithName(AppSettings.ServiceName)
                    .WithId(AppSettings.InstanceId)
                    .UseDefaultThreadPool(AppSettings.MaxConcurrency)
                    .UsePersistentStore(x =>
                    {
                        x.UseProperties = true;
                        x.UseJsonSerializer();

                        SetDatabaseProvider(x);

                        if (AppSettings.Clustering)
                        {
                            x.UseClustering(x =>
                            {
                                x.CheckinInterval = AppSettings.ClusteringCheckinInterval;
                                x.CheckinMisfireThreshold = AppSettings.ClusteringCheckinMisfireThreshold;
                            });
                        }
                    });

                _scheduler = await builder.BuildScheduler();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Initialize: Fail to InitializeScheduler");
                throw;
            }
        }

        private static void SetDatabaseProvider(PersistentStoreOptions options)
        {
            switch (AppSettings.DatabaseProvider)
            {
                case "SqlServer":
                    options.UseSqlServer(AppSettings.DatabaseConnectionString);
                    break;

                default:
                    throw new ApplicationException($"Database provider {AppSettings.DatabaseProvider} is not supported");
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

        private async Task AddCalendarSerializer()
        {
            try
            {
                _logger.LogInformation("Initialize: AddCalendarSerializer");

                JsonObjectSerializer.AddCalendarSerializer<HebrewCalendar>(new CustomCalendarSerializer(_logger));
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Initialize: Fail to AddCalendarSerializer");
                throw;
            }
        }

        #endregion Initialize Scheduler

        public static T Resolve<T>()
            where T : class
        {
            return Global.ServiceProvider.GetRequiredService(typeof(T)) as T;
        }
    }
}