using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Job.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Planar.Job
{
    public abstract class BaseJob
    {
        private BaseJobFactory _baseJobFactory = null!;
        private IConfiguration _configuration = null!;
        private JobExecutionContext _context = new JobExecutionContext();
        private bool _inConfiguration;
        private ILogger _logger = null!;
        private IServiceProvider _provider = null!;
        private Timer? _timer;
        private Version? _version;
        private AutoResetEvent _executeResetEvent = new AutoResetEvent(false);

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

        protected int? EffectedRows
        {
            get { return _baseJobFactory.EffectedRows; }
            set { _baseJobFactory.EffectedRows = value; }
        }

        protected int ExceptionCount => _baseJobFactory.ExceptionCount;
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

        protected Version? Version
        {
            get { return _version; }
            set
            {
                if (!_inConfiguration)
                {
                    throw new PlanarJobException("Version can be set only in Configure method");
                }

                _version = value;
            }
        }

        public IJobExecutionContext Context => _context;

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
                var connectTimeout = TimeSpan.FromSeconds(10);
                MqttClient.Connected += MqttClient_Connected;
                MqttClient.Start(_context.FireInstanceId, _context.JobPort).Wait();

                var isConnect = _executeResetEvent.WaitOne(connectTimeout);

                if (!isConnect)
                {
                    MqttClient.Stop().Wait();
                    Task.Delay(1000).Wait();
                    MqttClient.Start(_context.FireInstanceId, _context.JobPort).Wait();
                    _executeResetEvent = new AutoResetEvent(false);
                    isConnect = _executeResetEvent.WaitOne(connectTimeout);
                }

                if (isConnect)
                {
                    MqttClient.Ping().Wait();
                    MqttClient.Publish(MessageBrokerChannels.HealthCheck).Wait();
                }
                else
                {
                    throw new PlanarJobException("Fail to initialize message broker. Communication to planar fail");
                }
            }

            var mapper = new JobMapper(_logger);
            mapper.MapJobInstanceProperties(_context, this);
            LogVersion();

            try
            {
                var timeout = _context.TriggerDetails.Timeout;
                if (timeout == null || timeout.Value.TotalSeconds < 1) { timeout = TimeSpan.FromHours(2); }
                var timeoutms = timeout.Value.Add(TimeSpan.FromMinutes(3)).TotalMilliseconds;

                _timer = new Timer(timeoutms);
                _timer.Elapsed += TimerElapsed;
                _timer.Start();

                var task = ExecuteJob(_context);
                Task.WhenAll(task).Wait();
                _timer?.Stop();
                var mapperBack = new JobBackMapper(_logger, _baseJobFactory);
                mapperBack.MapJobInstancePropertiesBack(_context, this);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                SafeHandle(() => MqttClient.Connected -= MqttClient_Connected);
                SafeHandle(() => MqttClient.Stop().Wait());
                SafeHandle(() => _timer?.Dispose());
            }
        }

        private void MqttClient_Connected(object sender, EventArgs e)
        {
            _executeResetEvent.Set();
        }

        private void SafeHandle(Action action)
        {
            try
            {
                action.Invoke();
            }
            catch
            {
                // *** DO NOTHING *** //
            }
        }

        internal UnitTestResult ExecuteUnitTest(
            string json,
            Action<IConfigurationBuilder, IJobExecutionContext> configureAction,
            Action<IConfiguration, IServiceCollection, IJobExecutionContext> registerServicesAction)

        {
            InitializeBaseJobFactory(json);
            InitializeConfiguration(_context, configureAction);
            InitializeDepedencyInjection(_context, _baseJobFactory, registerServicesAction);

            Logger = ServiceProvider.GetRequiredService<ILogger>();
            LogVersion();
            ExecuteJob(_context).Wait();

            var result = new UnitTestResult
            {
                EffectedRows = EffectedRows,
                Log = BaseLogger.LogText
            };

            return result;
        }

        protected static void ValidateMaxLength(string? value, int length, string name)
        {
            if (value != null && value.Length > length)
            {
                throw new PlanarJobException($"{name} length is invalid. maximum length is {length}".Trim());
            }
        }

        protected void AddAggregateException(Exception ex, int maxItems = 25)
        {
            _baseJobFactory.AddAggregateException(ex, maxItems);
        }

        protected async Task AddAggregateExceptionAsync(Exception ex, int maxItems = 25)
        {
            await _baseJobFactory.AddAggregateExceptionAsync(ex, maxItems);
        }

        protected void CheckAggragateException()
        {
            _baseJobFactory.CheckAggragateException();
        }

        protected DateTime Now()
        {
            return _baseJobFactory.Now();
        }

        protected void PutJobData(string key, object? value)
        {
            PutJobDataAsync(key, value).Wait();
        }

        protected async Task PutJobDataAsync(string key, object? value)
        {
            ValidateSystemDataKey(key);
            ValidateMaxLength(Convert.ToString(value), 1000, "value");

            var data = _context.JobDetails.JobDataMap;
            if (data.Count >= Consts.MaximumJobDataItems && !ContainsKey(key, data))
            {
                throw new PlanarJobException($"Job data items exceeded maximum limit of {Consts.MaximumJobDataItems}");
            }

            await _baseJobFactory.PutJobDataAsync(key, value);
        }

        protected void PutTriggerData(string key, object? value)
        {
            PutTriggerDataAsync(key, value).Wait();
        }

        protected void RemoveJobData(string key)
        {
            _baseJobFactory.RemoveJobData(key);
        }

        protected async Task RemoveJobDataAsync(string key)
        {
            await _baseJobFactory.RemoveJobDataAsync(key);
        }

        protected void RemoveTriggerData(string key)
        {
            _baseJobFactory.RemoveTriggerData(key);
        }

        protected async Task RemoveTriggerDataAsync(string key)
        {
            await _baseJobFactory.RemoveTriggerDataAsync(key);
        }

        protected void ClearJobData()
        {
            _baseJobFactory.ClearJobData();
        }

        protected async Task ClearJobDataAsync()
        {
            await _baseJobFactory.ClearJobDataAsync();
        }

        protected void ClearTriggerData()
        {
            _baseJobFactory.ClearTriggerData();
        }

        protected async Task ClearTriggerDataAsync()
        {
            await _baseJobFactory.ClearTriggerDataAsync();
        }

        protected async Task PutTriggerDataAsync(string key, object? value)
        {
            ValidateSystemDataKey(key);
            ValidateMaxLength(Convert.ToString(value), 1000, "value");

            var data = _context.TriggerDetails.TriggerDataMap;
            if (data.Count >= Consts.MaximumJobDataItems && !ContainsKey(key, data))
            {
                throw new PlanarJobException($"Trigger data items exceeded maximum limit of {Consts.MaximumJobDataItems}");
            }

            await _baseJobFactory.PutTriggerDataAsync(key, value);
        }

        protected void UpdateProgress(byte value)
        {
            _baseJobFactory.UpdateProgress(value);
        }

        protected void UpdateProgress(long current, long total)
        {
            _baseJobFactory.UpdateProgress(current, total);
        }

        protected async Task UpdateProgressAsync(byte value)
        {
            await _baseJobFactory.UpdateProgressAsync(value);
        }

        protected async Task UpdateProgressAsync(long current, long total)
        {
            await _baseJobFactory.UpdateProgressAsync(current, total);
        }

        private static void FilterJobData(IDataMap dictionary)
        {
            var toRemove = dictionary
                .Where(k => !Consts.IsDataKeyValid(k.Key))
                .Select(pair => pair.Key)
                .ToList();

            foreach (var key in toRemove)
            {
                ((DataMap)dictionary).Remove(key);
            }
        }

        private static void ValidateMinLength(string? value, int length, string name)
        {
            if (value != null && value.Length < length)
            {
                throw new PlanarJobException($"{name} length is invalid. minimum length is {length}".Trim());
            }
        }

        private static void ValidateRange(string? value, int from, int to, string name)
        {
            ValidateMinLength(value, from, name);
            ValidateMaxLength(value, to, name);
        }

        private static void ValidateSystemDataKey(string key)
        {
            if (key.StartsWith(Consts.ConstPrefix))
            {
                throw new PlanarJobException($"date '{key}' is system data key and it should not be modified");
            }

            if (!Consts.IsDataKeyValid(key))
            {
                throw new PlanarJobException($"date '{key}' is invalid data key");
            }

            ValidateRange(key, 1, 100, "key");

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new PlanarJobException("key is required");
            }
        }

        private bool ContainsKey(string key, IDataMap data)
        {
            return data.Any(k => string.Equals(k.Key, key, StringComparison.OrdinalIgnoreCase));
        }

        private void HandleException(Exception ex)
        {
            if (ex is AggregateException aggregateException && aggregateException.InnerExceptions.Count > 0)
            {
                HandleException(aggregateException.InnerExceptions[0]);
                return;
            }

            var text = _baseJobFactory.ReportException(ex);
            if (PlanarJob.Mode == RunningMode.Debug)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(text);
                Console.ResetColor();
            }
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

                // Debug mode need to manually read settings file/s
                if (PlanarJob.Mode == RunningMode.Debug)
                {
                    var settings = JobSettingsLoader.LoadJobSettings(null, ctx.JobSettings);
                    ctx.JobSettings = ctx.JobSettings.Merge(settings);
                    ctx.JobSettings = new Dictionary<string, string?>(settings);
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
            _inConfiguration = true;
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(context.JobSettings);
            configureAction.Invoke(builder, context);
            Configuration = builder.Build();
            _inConfiguration = false;
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

        private void LogVersion()
        {
            if (Version == null) { return; }
            Logger.LogInformation("job version: {Version}", Version);
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var ex = new PlanarJobException("Execution timeout. Terminate application");
                HandleException(ex);
            }
            catch
            {
                // *** DO NOTHING ***
            }

            try
            {
                MqttClient.Stop().Wait();
                _timer?.Dispose();
            }
            catch
            {
                // *** DO NOTHING ***
            }

            Environment.Exit(-1);
        }
    }
}