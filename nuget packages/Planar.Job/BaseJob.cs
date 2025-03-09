﻿using Microsoft.Extensions.Configuration;
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
    public abstract class BaseJob : IBaseJob
    {
#if NETSTANDARD2_0
        private BaseJobFactory _baseJobFactory = null;
        private IConfiguration _configuration = null;
        private ILogger _logger = null;
        private IServiceProvider _provider = null;
        private Timer _timer;
        private Version _version;
        private AutoResetEvent _executeResetEvent;
#else
        private BaseJobFactory _baseJobFactory = null!;
        private IConfiguration _configuration = null!;
        private ILogger _logger = null!;
        private IServiceProvider _provider = null!;
        private Timer? _timer;
        private Version? _version;
        private AutoResetEvent? _executeResetEvent;
#endif

        private JobExecutionContext _context = new JobExecutionContext();
        private bool _inConfiguration;

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

        public int? EffectedRows
        {
            get { return _baseJobFactory.EffectedRows; }
            set { _baseJobFactory.EffectedRows = value; }
        }

        public int ExceptionCount => _baseJobFactory.ExceptionCount;

        public TimeSpan JobRunTime => _baseJobFactory.JobRunTime;

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

#if NETSTANDARD2_0

        protected Version Version
#else
        protected Version? Version
#endif
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

        internal async Task<bool> Execute(string json)
        {
            Action<IConfigurationBuilder, IJobExecutionContext> configureAction = Configure;
            Action<IConfiguration, IServiceCollection, IJobExecutionContext> registerServicesAction = RegisterServices;

#if NETSTANDARD2_0
            Exception initializeException = null;
            try { InitializeBaseJobFactory(json); } catch (Exception ex) { initializeException = ex; }
            try { InitializeConfiguration(_context, configureAction); } catch (Exception ex) { if (initializeException == null) { initializeException = ex; } }
            try { InitializeDepedencyInjection(_context, _baseJobFactory, registerServicesAction); } catch (Exception ex) { if (initializeException == null) { initializeException = ex; } }
#else
            Exception? initializeException = null;
            try { InitializeBaseJobFactory(json); } catch (Exception ex) { initializeException = ex; }
            try { InitializeConfiguration(_context, configureAction); } catch (Exception ex) { initializeException ??= ex; }
            try { InitializeDepedencyInjection(_context, _baseJobFactory, registerServicesAction); } catch (Exception ex) { initializeException ??= ex; }
#endif

            try
            {
                await OpenMqttConnection();

                Logger = ServiceProvider.GetRequiredService<ILogger>();

                var mapper = new JobMapper(_logger);
                mapper.MapJobInstanceProperties(_context, this);
                LogVersion();

                if (initializeException != null) { throw initializeException; }

                var timeout = _context.TriggerDetails.Timeout;
                if (timeout == null || timeout.Value.TotalSeconds < 1) { timeout = TimeSpan.FromHours(2); }
                var timeoutms = timeout.Value.Add(TimeSpan.FromMinutes(3)).TotalMilliseconds;
                _timer = new Timer(timeoutms);
                _timer.Elapsed += TimerElapsed;
                _timer.Start();

                var task = ExecuteJob(_context);
                await Task.WhenAll(task);
                _timer?.Stop();
                return true;
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return false;
            }
            finally
            {
                SafeHandle(() =>
                {
                    var mapperBack = new JobBackMapper(_logger, _baseJobFactory);
                    mapperBack.MapJobInstancePropertiesBack(_context, this);
                });

                SafeHandle(() => _timer?.Dispose());
                SafeHandle(() => MqttClient.Connected -= MqttClient_Connected);
                await SafeHandleAsync(MqttClient.StopAsync);
            }
        }

        internal async Task PrintDebugSummary(bool success)
        {
            if (PlanarJob.Mode == RunningMode.Debug)
            {
                var status = success ? "Success" : "Fail";
                await Console.Out.WriteLineAsync("---------------------------------------");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                await Console.Out.WriteLineAsync(" Summary");
                await Console.Out.WriteLineAsync(" =======");
                await Console.Out.WriteLineAsync($" - Status: {status}");
                await Console.Out.WriteLineAsync($" - Effected Rows: {EffectedRows}");
                await Console.Out.WriteLineAsync($" - Exception Count: {ExceptionCount}");
                await Console.Out.WriteLineAsync($" - Fire Time: {_baseJobFactory.Context.FireTime.DateTime}");
                await Console.Out.WriteLineAsync($" - Job Run Time: {FormatTimeSpan(JobRunTime)}");
                Console.ResetColor();
            }
        }

        internal async Task PrintMergedData()
        {
            if (PlanarJob.Mode == RunningMode.Debug)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                await Console.Out.WriteLineAsync(">> Merged Data");
                await Console.Out.WriteLineAsync("---------------------------------------");

                foreach (var item in _baseJobFactory.Context.MergedJobDataMap)
                {
                    await Console.Out.WriteLineAsync($" {item.Key}: {item.Value}");
                }

                Console.ResetColor();
                await Console.Out.WriteLineAsync("---------------------------------------");
            }
        }

        private static string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
            {
                return $"{timeSpan:\\(d\\)\\ hh\\:mm\\:ss}";
            }

            return $"{timeSpan:hh\\:mm\\:ss}";
        }

        private async Task OpenMqttConnection()
        {
            if (PlanarJob.Mode == RunningMode.Release)
            {
                var connectTimeout = TimeSpan.FromSeconds(6);
                MqttClient.Connected += MqttClient_Connected;

                for (int i = 0; i < 3; i++)
                {
                    if (await SafeStartMqttClient(connectTimeout)) { return; }
                }

                // mqtt failover by http to planar service
                for (int i = 0; i < 3; i++)
                {
                    if (await SafeStartFailOverProxy()) { return; }
                }

                throw new PlanarJobException("Fail to initialize message broker. Communication to planar fail");
            }
        }

        private async Task<bool> SafeStartFailOverProxy()
        {
            try
            {
                // TODO: change the port to variable
                MqttClient.StartFailOver(_context.FireInstanceId, 2306);
                await MqttClient.PingAsync();
                await MqttClient.PublishAsync(MessageBrokerChannels.HealthCheck);
                return true;
            }
            catch
            {
                await MqttClient.StopAsync();
                return false;
            }
        }

        private async Task<bool> SafeStartMqttClient(TimeSpan connectTimeout)
        {
            try
            {
                _executeResetEvent = new AutoResetEvent(false);
                await MqttClient.StartAsync(_context.FireInstanceId, _context.JobPort);
                _executeResetEvent.WaitOne(connectTimeout);
                await MqttClient.PingAsync();

                for (int i = 0; i < 3; i++)
                {
                    await MqttClient.PublishAsync(MessageBrokerChannels.HealthCheck);
                    await Task.Delay(50);
                }
                return true;
            }
            catch
            {
                await MqttClient.StopAsync();
                await Task.Delay(500);
                return false;
            }
        }

        private void MqttClient_Connected(object sender, EventArgs e)
        {
            _executeResetEvent?.Set();
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

        private async Task SafeHandleAsync(Func<Task> action)
        {
            try
            {
                await action.Invoke();
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

#if NETSTANDARD2_0

        protected static void ValidateMaxLength(string value, int length, string name)
#else
        protected static void ValidateMaxLength(string? value, int length, string name)
#endif
        {
            if (value != null && value.Length > length)
            {
                throw new PlanarJobException($"{name} length is invalid. maximum length is {length}".Trim());
            }
        }

        public void AddAggregateException(Exception ex, int maxItems = 25)
        {
            _baseJobFactory.AddAggregateException(ex, maxItems);
        }

        public async Task AddAggregateExceptionAsync(Exception ex, int maxItems = 25)
        {
            await _baseJobFactory.AddAggregateExceptionAsync(ex, maxItems);
        }

        public void CheckAggragateException()
        {
            _baseJobFactory.CheckAggragateException();
        }

        public DateTime Now()
        {
            return _baseJobFactory.Now();
        }

#if NETSTANDARD2_0

        public void PutJobData(string key, object value)
#else
        public void PutJobData(string key, object? value)
#endif
        {
            PutJobDataAsync(key, value).Wait();
        }

#if NETSTANDARD2_0

        public async Task PutJobDataAsync(string key, object value)
#else
        public async Task PutJobDataAsync(string key, object? value)
#endif
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

#if NETSTANDARD2_0

        public void PutTriggerData(string key, object value)
#else
        public void PutTriggerData(string key, object? value)
#endif
        {
            PutTriggerDataAsync(key, value).Wait();
        }

        public void RemoveJobData(string key)
        {
            _baseJobFactory.RemoveJobData(key);
        }

        public async Task RemoveJobDataAsync(string key)
        {
            await _baseJobFactory.RemoveJobDataAsync(key);
        }

        public void RemoveTriggerData(string key)
        {
            _baseJobFactory.RemoveTriggerData(key);
        }

        public async Task RemoveTriggerDataAsync(string key)
        {
            await _baseJobFactory.RemoveTriggerDataAsync(key);
        }

        public void ClearJobData()
        {
            _baseJobFactory.ClearJobData();
        }

        public async Task ClearJobDataAsync()
        {
            await _baseJobFactory.ClearJobDataAsync();
        }

        public void ClearTriggerData()
        {
            _baseJobFactory.ClearTriggerData();
        }

        public async Task ClearTriggerDataAsync()
        {
            await _baseJobFactory.ClearTriggerDataAsync();
        }

#if NETSTANDARD2_0

        public async Task PutTriggerDataAsync(string key, object value)
#else
        public async Task PutTriggerDataAsync(string key, object? value)
#endif
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

        public void UpdateProgress(byte value)
        {
            _baseJobFactory.UpdateProgress(value);
        }

        public void UpdateProgress(long current, long total)
        {
            _baseJobFactory.UpdateProgress(current, total);
        }

        public async Task UpdateProgressAsync(byte value)
        {
            await _baseJobFactory.UpdateProgressAsync(value);
        }

        public async Task UpdateProgressAsync(long current, long total)
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

#if NETSTANDARD2_0

        private static void ValidateMinLength(string value, int length, string name)
#else
        private static void ValidateMinLength(string? value, int length, string name)
#endif
        {
            if (value != null && value.Length < length)
            {
                throw new PlanarJobException($"{name} length is invalid. minimum length is {length}".Trim());
            }
        }

#if NETSTANDARD2_0

        private static void ValidateRange(string value, int from, int to, string name)
#else
        private static void ValidateRange(string? value, int from, int to, string name)
#endif

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

#if NETSTANDARD2_0
                    ctx.JobSettings = new Dictionary<string, string>(settings);
#else
                    ctx.JobSettings = new Dictionary<string, string?>(settings);
#endif
                }

                _context = ctx;
            }
            catch (Exception ex)
            {
                throw new PlanarJobException("Fail to initialize job", ex);
            }
        }

        private void InitializeConfiguration(JobExecutionContext context, Action<IConfigurationBuilder, IJobExecutionContext> configureAction)
        {
            _inConfiguration = true;
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(context.JobSettings);

            try
            {
                configureAction.Invoke(builder, context);
            }
            catch (Exception ex)
            {
                throw new PlanarJobException($"Fail to initialize job at {nameof(Configure)}(...) method implementation", ex);
            }
            finally
            {
                _inConfiguration = false;
                Configuration = builder.Build();
            }
        }

        private void InitializeDepedencyInjection(JobExecutionContext context, BaseJobFactory baseJobFactory, Action<IConfiguration, IServiceCollection, IJobExecutionContext> registerServicesAction)
        {
            var services = new ServiceCollection();
            services.AddSingleton(Configuration);
            services.AddSingleton<IJobExecutionContext>(context);
            services.AddSingleton<IBaseJob>(baseJobFactory);
            services.AddSingleton<ILogger, PlanarLogger>();
            services.AddSingleton(typeof(ILogger<>), typeof(PlanarLogger<>));

            try
            {
                registerServicesAction.Invoke(Configuration, services, context);
            }
            catch (Exception ex)
            {
                throw new PlanarJobException($"Fail to initialize job at {nameof(RegisterServices)}(...) method implementation", ex);
            }
            finally
            {
                _provider = services.BuildServiceProvider();
            }
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
                MqttClient.StopAsync().Wait();
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