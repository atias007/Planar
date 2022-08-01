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

        public MainService(IServiceProvider serviceProvider)
        {
            _logger = serviceProvider.GetService<ILogger<MainService>>();
            Global.ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider)); ;
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

        public static void Shutdown()
        {
            if (Global.ServiceProvider.GetService(typeof(IHostApplicationLifetime)) is IHostApplicationLifetime app) { app.StopApplication(); }
            Global.Clear();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var mainTask = Run();
            var waiter = new CancellationTokenAwaiter(stoppingToken);

            waiter.OnCompleted(async () =>
            {
                _logger.LogInformation("IsCancellationRequested = true");
                RemoveSchedulerCluster().Wait();
                await _scheduler?.Shutdown(true);
            });

            return Task.CompletedTask;
        }

        public async Task Run()
        {
            _logger.LogInformation("Service environment: {Environment}", Global.Environment);

            await LoadGlobalParametersInner();

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

        private async Task LoadGlobalParametersInner()
        {
            try
            {
                _logger.LogInformation("Initialize: LoadGlobalParameters");
                await LoadGlobalParameters();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Initialize: Fail to LoadGlobalParameters");
                throw;
            }
        }

        public static async Task LoadGlobalParameters()
        {
            var dal = Resolve<DataLayer>();
            var prms = await dal.GetAllGlobalParameter();
            var dict = prms.ToDictionary(p => p.ParamKey, p => p.ParamValue);
            Global.Parameters = dict;
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
                await _scheduler.Start();
                _logger.LogInformation("Initialize: Scheduler is online :))");
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
            var dal = Resolve<DataLayer>();
            var cluster = new ClusterNode
            {
                Server = Environment.MachineName,
                Port = AppSettings.HttpPort,
                InstanceId = _scheduler.SchedulerInstanceId
            };

            await dal.RemoveClusterNode(cluster);
        }

        private async Task JoinToCluster()
        {
            try
            {
                _logger.LogInformation("Initialize: AddSchedulerCluster");

                var dal = Resolve<DataLayer>();
                var nodes = await dal.GetClusterNodes();
                var util = new ClusterUtil(dal, _logger);
                ClusterUtil.ValidateClusterConflict(nodes);

                if (await util.HealthCheck(nodes))
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
                throw;
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
            return Global.ServiceProvider.GetService(typeof(T)) as T;
        }
    }
}