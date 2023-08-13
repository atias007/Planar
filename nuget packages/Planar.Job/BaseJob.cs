using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Job.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Job
{
    public abstract class BaseJob
    {
        private BaseJobFactory _baseJobFactory = null!;
        private IConfiguration _configuration = null!;
        private JobExecutionContext _context = new JobExecutionContext();

        private ILogger _logger = null!;
        private IServiceProvider _provider = null!;

        protected IConfiguration Configuration
        {
            get
            {
                return _configuration;
            }
            private set
            {
                _configuration = value;
            }
        }

        protected TimeSpan JobRunTime => _baseJobFactory.JobRunTime;

        protected ILogger Logger
        {
            get
            {
                return _logger;
            }
            private set
            {
                _logger = value;
            }
        }

        protected IServiceProvider ServiceProvider => _provider;

        public abstract void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context);

        public abstract Task ExecuteJob(IJobExecutionContext context);

        public abstract void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context);

        internal void Execute(string json)
        {
            Action<IConfigurationBuilder, IJobExecutionContext> configureAction = Configure;
            Action<IConfiguration, IServiceCollection, IJobExecutionContext> registerServicesAction = RegisterServices;

            InitializeBaseJobFactory(json);
            InitializeConfiguration(_context, configureAction);
            InitializeDepedencyInjection(_context, _baseJobFactory, registerServicesAction);

            Logger = ServiceProvider.GetRequiredService<ILogger>();

            if (PlanarJob.Mode == RunningMode.Release)
            {
                MqttClient.Start(_context.FireInstanceId).ConfigureAwait(false).GetAwaiter().GetResult();
                SpinWait.SpinUntil(() => MqttClient.IsConnected, TimeSpan.FromSeconds(5));
                if (MqttClient.IsConnected)
                {
                    MqttClient.Ping().ConfigureAwait(false).GetAwaiter().GetResult();
                    MqttClient.Publish(MessageBrokerChannels.HealthCheck).ConfigureAwait(false).GetAwaiter().GetResult();
                }
                else
                {
                    throw new PlanarJobException("Fail to initialize message broker");
                }
            }

            var mapper = new JobMapper(_logger);
            mapper.MapJobInstanceProperties(_context, this);

            try
            {
                ExecuteJob(_context).ConfigureAwait(false).GetAwaiter().GetResult();
                MapJobInstancePropertiesBack(_context);
            }
            catch (Exception ex)
            {
                var text = GetExceptionText(ex);
                _baseJobFactory.ReportExceptionText(text);
                if (PlanarJob.Mode == RunningMode.Debug)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine(text);
                    Console.ResetColor();
                }
            }
            finally
            {
                MqttClient.Stop().ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        internal void ExecuteUnitTest(
            string json,
            Action<IConfigurationBuilder, IJobExecutionContext> configureAction,
            Action<IConfiguration, IServiceCollection, IJobExecutionContext> registerServicesAction)

        {
            InitializeBaseJobFactory(json);
            InitializeConfiguration(_context, configureAction);
            InitializeDepedencyInjection(_context, _baseJobFactory, registerServicesAction);

            Logger = ServiceProvider.GetRequiredService<ILogger>();

            ExecuteJob(_context).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        protected void AddAggregateException(Exception ex)
        {
            _baseJobFactory.AddAggregateException(ex);
        }

        protected void CheckAggragateException()
        {
            _baseJobFactory.CheckAggragateException();
        }

        protected int? GetEffectedRows()
        {
            return _baseJobFactory.GetEffectedRows();
        }

        protected void IncreaseEffectedRows(int delta = 1)
        {
            _baseJobFactory.IncreaseEffectedRows(delta);
        }

        protected void MapJobInstancePropertiesBack(IJobExecutionContext context)
        {
            try
            {
                if (context == null) { return; }

                var propInfo = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
                foreach (var prop in propInfo)
                {
                    if (prop.Name.StartsWith(Consts.ConstPrefix)) { continue; }
                    SafePutData(context, prop);
                }
            }
            catch (Exception ex)
            {
                var source = nameof(MapJobInstancePropertiesBack);
                _logger.LogError(ex, "Fail at {Source} with job {Group}.{Name}", source, context.JobDetails.Key.Group, context.JobDetails.Key.Name);
                throw;
            }
        }

        protected DateTime Now()
        {
            return _baseJobFactory.Now();
        }

        protected void PutJobData(string key, object? value)
        {
            _baseJobFactory.PutJobData(key, value);
        }

        protected void PutTriggerData(string key, object? value)
        {
            _baseJobFactory.PutTriggerData(key, value);
        }

        protected void SetEffectedRows(int value)
        {
            _baseJobFactory.SetEffectedRows(value);
        }

        protected void UpdateProgress(byte value)
        {
            _baseJobFactory.UpdateProgress(value);
        }

        protected void UpdateProgress(int current, int total)
        {
            _baseJobFactory.UpdateProgress(current, total);
        }

        private static void FilterJobData(IDataMap dictionary)
        {
            foreach (var item in Consts.AllDataKeys)
            {
                if (dictionary.ContainsKey(item))
                {
                    ((DataMap)dictionary).Remove(item);
                }
            }
        }

        private static string GetExceptionText(Exception ex)
        {
            var lines = ex.ToString().Split('\n');
            var filterLines = lines.Where(l => !l.Contains($"{nameof(Planar)}.{nameof(Job)}\\{nameof(BaseJob)}.cs"));
            var text = string.Join("\n", filterLines);
            return text;
        }

        private void InitializeBaseJobFactory(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    Converters =
                    {
                        new TypeMappingConverter<IJobDetail, JobDetail>(),
                        new TypeMappingConverter<ITriggerDetail, TriggerDetail>(),
                        new TypeMappingConverter<IKey, Key>(),
                        new DataMapConvertor()
                    }
                };

                var ctx = JsonSerializer.Deserialize<JobExecutionContext>(json, options) ??
                    throw new PlanarJobException("Fail to initialize JobExecutionContext from json (error 7379)");

                _baseJobFactory = new BaseJobFactory(ctx);

                FilterJobData(ctx.MergedJobDataMap);
                FilterJobData(ctx.JobDetails.JobDataMap);
                FilterJobData(ctx.TriggerDetails.TriggerDataMap);

                if (PlanarJob.Mode == RunningMode.Debug)
                {
                    ctx.JobSettings = new Dictionary<string, string?>(JobSettingsLoader.LoadJobSettings(ctx.JobSettings));
                }

                _context = ctx;
            }
            catch (Exception ex)
            {
                throw new PlanarJobException("Fail to deserialize job execution context at BaseJob.InitializeBaseJobFactory(string)", ex);
            }
        }

        private void InitializeConfiguration(JobExecutionContext context, Action<IConfigurationBuilder, IJobExecutionContext> configureAction)
        {
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(context.JobSettings);
            configureAction.Invoke(builder, context);
            Configuration = builder.Build();
        }

        private void InitializeDepedencyInjection(JobExecutionContext context, BaseJobFactory baseJobFactory, Action<IConfiguration, IServiceCollection, IJobExecutionContext> registerServicesAction)
        {
            var services = new ServiceCollection();
            services.AddSingleton(Configuration);
            services.AddSingleton<IJobExecutionContext>(context);
            services.AddSingleton<IBaseJob>(baseJobFactory);
            services.AddSingleton<ILogger, PlanarLogger>();
            services.AddSingleton(typeof(ILogger<>), typeof(PlanarLogger<>));
            registerServicesAction.Invoke(Configuration, services, context);
            _provider = services.BuildServiceProvider();
        }

        private void SafePutData(IJobExecutionContext context, PropertyInfo prop)
        {
            var jobAttribute = prop.GetCustomAttribute<JobDataAttribute>();
            var triggerAttribute = prop.GetCustomAttribute<TriggerDataAttribute>();
            var ignoreAttribute = prop.GetCustomAttribute<IgnoreDataMapAttribute>();

            if (ignoreAttribute != null)
            {
                var jobKey = context.JobDetails.Key;

                _logger.LogDebug("ATTENTION: Ignore map back property {PropertyName} of job '{JobGroup}.{JobName}' to data map",
                    prop.Name,
                    jobKey.Group,
                    jobKey.Name);

                return;
            }

            if (jobAttribute != null)
            {
                SafePutJobDataMap(context, prop);
            }

            if (triggerAttribute != null)
            {
                SafePutTiggerDataMap(context, prop);
            }

            if (jobAttribute == null && triggerAttribute == null)
            {
                if (context.JobDetails.JobDataMap.ContainsKey(prop.Name))
                {
                    SafePutJobDataMap(context, prop);
                }

                if (context.TriggerDetails.TriggerDataMap.ContainsKey(prop.Name))
                {
                    SafePutTiggerDataMap(context, prop);
                }
            }
        }

        private void SafePutJobDataMap(IJobExecutionContext context, PropertyInfo prop)
        {
            string? value = null;
            try
            {
                if (!Consts.IsDataKeyValid(prop.Name))
                {
                    throw new PlanarJobException($"the data key {prop.Name} in invalid");
                }

                value = PlanarConvert.ToString(prop.GetValue(this));
                PutJobData(prop.Name, value);
            }
            catch (Exception ex)
            {
                var jobKey = context.JobDetails.Key;
                _logger.LogWarning(ex,
                    "Fail to save back value {Value} from property {Name} to JobDetails at job {JobGroup}.{JobName}",
                    value, prop.Name, jobKey.Group, jobKey.Name);
            }
        }

        private void SafePutTiggerDataMap(IJobExecutionContext context, PropertyInfo prop)
        {
            string? value = null;
            try
            {
                if (!Consts.IsDataKeyValid(prop.Name))
                {
                    throw new PlanarJobException($"the data key {prop.Name} in invalid");
                }

                value = PlanarConvert.ToString(prop.GetValue(this));
                PutTriggerData(prop.Name, value);
            }
            catch (Exception ex)
            {
                var jobKey = context.JobDetails.Key;
                _logger.LogWarning(ex,
                    "Fail to save back value {Value} from property {Name} to TriggerDetails at job {JobGroup}.{JobName}",
                    value, prop.Name, jobKey.Group, jobKey.Name);
            }
        }
    }
}