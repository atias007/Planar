using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Planar.Calendar.Hebrew;
using Planar.Common;
using Planar.Service.Data;
using Planar.Service.General;
using Planar.Service.List;
using Planar.Service.Monitor;
using Planar.Service.SystemJobs;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using Quartz.Logging;
using Quartz.Simpl;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service
{
    public class MainService : BackgroundService
    {
        private static IScheduler _scheduler;
        private static IConfiguration _config;
        private readonly ILogger<MainService> _logger;

        public MainService(IConfiguration config, IServiceProvider serviceProvider)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
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
                await _scheduler.Shutdown(true);
            });

            return Task.CompletedTask;
        }

        public async Task Run()
        {
            await LoadGlobalParametersInner();

            await SetQuartzLogProvider();

            await AddCalendarSerializer();

            var quartzConfig = LoadQuartzConfiguration();

            await InitializeScheduler(quartzConfig);

            await AddJobListeners();

            await AddCalendars();

            AddMonitorHooks();

            await ScheduleSystemJobs();

            await StartScheduler();
        }

        #region Initialize Scheduler

        private async Task LoadGlobalParametersInner()
        {
            await LoadGlobalParameters();
            _logger.LogInformation("Service environment: {Environment}", Global.Environment);
        }

        public static async Task LoadGlobalParameters()
        {
            Global.Environment = _config.GetValue<string>(Consts.EnvironmentVariableKey);

            var dal = Resolve<DataLayer>();
            var prms = await dal.GetAllGlobalParameter();
            var dict = prms.ToDictionary(p => p.ParamKey, p => p.ParamValue);
            Global.Parameters = dict;
        }

        public static async Task ScheduleSystemJobs()
        {
            await PersistDataJob.Schedule(Scheduler);
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

        private void AddMonitorHooks()
        {
            try
            {
                ServiceUtil.LoadMonitorHooks(_logger);
                MonitorUtil.Load();
                MonitorUtil.Validate(_logger);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Initialize: Fail to AddMonitorHooks");
                throw;
            }
        }

        private async Task InitializeScheduler(NameValueCollection config)
        {
            try
            {
                _logger.LogInformation("Initialize: InitializeScheduler");

                var factory = new StdSchedulerFactory(config);
                _scheduler = await factory.GetScheduler();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Initialize: Fail to InitializeScheduler");
                throw;
            }
        }

        private NameValueCollection LoadQuartzConfiguration()
        {
            try
            {
                var result = new NameValueCollection
                {
                    { "quartz.scheduler.instanceName", "PlanarScheduler" },
                    { "quartz.jobStore.type", "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz" },
                    { "quartz.jobStore.driverDelegateType", "Quartz.Impl.AdoJobStore.StdAdoDelegate, Quartz" },
                    { "quartz.jobStore.tablePrefix", "QRTZ_" },
                    { "quartz.jobStore.dataSource", "myDS" },
                    { "quartz.dataSource.myDS.connectionString", AppSettings.DatabaseConnectionString },
                    { "quartz.dataSource.myDS.provider", "SqlServer" },
                    { "quartz.serializer.type", "json" },
                    { "quartz.threadPool.maxConcurrency", AppSettings.MaxConcurrency.ToString() }
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Initialize: Fail to LoadQuartzConfiguration");
                throw;
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