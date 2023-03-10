using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Job.Logger;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Planar.Job
{
    public abstract class BaseJob
    {
        private JobExecutionContext _context = new JobExecutionContext();
        private bool? _isNowOverrideValueExists;
        private MessageBroker _messageBroker = MessageBroker.Empty;
        private DateTime? _nowOverrideValue;
        private IServiceProvider? _provider;

        private IConfiguration? _configuration;

        protected IConfiguration Configuration
        {
            get
            {
                if (_configuration == null)
                {
                    throw new ArgumentException(nameof(Configuration));
                }

                return _configuration;
            }
            private set
            {
                _configuration = value;
            }
        }

        private ILogger? _logger;

        protected ILogger Logger
        {
            get
            {
                if (_logger == null)
                {
                    throw new ArgumentException(nameof(Logger));
                }

                return _logger;
            }
            private set
            {
                _logger = value;
            }
        }

        protected IServiceProvider ServiceProvider
        {
            get
            {
                if (_provider == null) { throw new ArgumentNullException(nameof(ServiceProvider)); }
                return _provider;
            }
        }

        public abstract void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context);

        public abstract Task ExecuteJob(IJobExecutionContext context);

        public abstract void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context);

        internal Task Execute(ref object messageBroker)
        {
            Action<IConfigurationBuilder, IJobExecutionContext> configureAction = Configure;
            Action<IConfiguration, IServiceCollection, IJobExecutionContext> registerServicesAction = RegisterServices;

            InitializeMessageBroker(messageBroker);
            InitializeConfiguration(_context, configureAction);
            InitializeDepedencyInjection(_context, _messageBroker, registerServicesAction);

            Logger = ServiceProvider.GetRequiredService<ILogger>();

            return ExecuteJob(_context)
                .ContinueWith(t =>
                {
                    if (t.Exception != null) { throw t.Exception; }
                });
        }

        internal Task ExecuteUnitTest(
            ref object messageBroker,
            Action<IConfigurationBuilder, IJobExecutionContext> configureAction,
            Action<IConfiguration, IServiceCollection, IJobExecutionContext> registerServicesAction)

        {
            InitializeMessageBroker(messageBroker);
            InitializeConfiguration(_context, configureAction);
            InitializeDepedencyInjection(_context, _messageBroker, registerServicesAction);

            Logger = ServiceProvider.GetRequiredService<ILogger>();

            return ExecuteJob(_context)
                .ContinueWith(t =>
                {
                    if (t.Exception != null) { throw t.Exception; }
                });
        }

        protected void AddAggragateException(Exception ex)
        {
            var message = new ExceptionDto(ex);
            _messageBroker?.Publish(MessageBrokerChannels.AddAggragateException, message);
        }

        protected void CheckAggragateException()
        {
            var text = _messageBroker?.Publish(MessageBrokerChannels.GetExceptionsText);
            if (string.IsNullOrEmpty(text) == false)
            {
                var ex = new PlanarJobAggragateException(text);
                throw ex;
            }
        }

        protected bool CheckIfStopRequest()
        {
            var text = _messageBroker?.Publish(MessageBrokerChannels.CheckIfStopRequest);
            _ = bool.TryParse(text, out var stop);
            return stop;
        }

        protected void FailOnStopRequest(Action? stopHandle = default)
        {
            if (stopHandle != default)
            {
                stopHandle.Invoke();
            }

            _messageBroker?.Publish(MessageBrokerChannels.FailOnStopRequest);
        }

        protected T GetData<T>(string key)
        {
            var value = _messageBroker?.Publish(MessageBrokerChannels.GetData, key);
            var result = (T)Convert.ChangeType(value, typeof(T));
            return result;
        }

        protected string GetData(string key)
        {
            return GetData<string>(key);
        }

        protected int? GetEffectedRows()
        {
            var text = _messageBroker?.Publish(MessageBrokerChannels.GetEffectedRows);
            _ = int.TryParse(text, out var rows);
            return rows;
        }

        protected void IncreaseEffectedRows(int delta = 1)
        {
            _messageBroker?.Publish(MessageBrokerChannels.IncreaseEffectedRows, delta);
        }

        protected bool IsDataExists(string key)
        {
            var text = _messageBroker?.Publish(MessageBrokerChannels.DataContainsKey, key);
            _ = bool.TryParse(text, out var result);
            return result;
        }

        protected TimeSpan JobRunTime
        {
            get
            {
                var text = _messageBroker?.Publish(MessageBrokerChannels.JobRunTime);
                var success = double.TryParse(text, out var result);
                if (success)
                {
                    return TimeSpan.FromMilliseconds(result);
                }
                else
                {
                    return TimeSpan.Zero;
                }
            }
        }

        protected DateTime Now()
        {
            if (_isNowOverrideValueExists == null)
            {
                _isNowOverrideValueExists = IsDataExists(Consts.NowOverrideValue);
                if (_isNowOverrideValueExists.GetValueOrDefault())
                {
                    var value = GetData(Consts.NowOverrideValue);
                    if (DateTime.TryParse(value, out DateTime dateValue))
                    {
                        _nowOverrideValue = dateValue;
                    }
                }
            }

            if (_nowOverrideValue.HasValue)
            {
                return _nowOverrideValue.Value;
            }
            else
            {
                return DateTime.Now;
            }
        }

        protected void PutJobData(string key, object value)
        {
            var message = new { Key = key, Value = value };
            _messageBroker?.Publish(MessageBrokerChannels.PutJobData, message);
        }

        protected void PutTriggerData(string key, object value)
        {
            var message = new { Key = key, Value = value };
            _messageBroker?.Publish(MessageBrokerChannels.PutTriggerData, message);
        }

        protected void SetEffectedRows(int value)
        {
            _messageBroker?.Publish(MessageBrokerChannels.SetEffectedRows, value);
        }

        protected void UpdateProgress(byte value)
        {
            if (value > 100) { value = 100; }
            if (value < 0) { value = 0; }
            _messageBroker?.Publish(MessageBrokerChannels.UpdateProgress, value);
        }

        protected void UpdateProgress(int current, int total)
        {
            var percentage = 1.0 * current / total;
            var value = Convert.ToByte(percentage * 100);
            UpdateProgress(value);
        }

        private void InitializeConfiguration(JobExecutionContext context, Action<IConfigurationBuilder, IJobExecutionContext> configureAction)
        {
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(context.JobSettings);
            configureAction.Invoke(builder, context);
            Configuration = builder.Build();
        }

        private void InitializeDepedencyInjection(JobExecutionContext context, MessageBroker messageBroker, Action<IConfiguration, IServiceCollection, IJobExecutionContext> registerServicesAction)
        {
            var services = new ServiceCollection();
            services.AddSingleton(Configuration);
            services.AddSingleton<IJobExecutionContext>(context);
            services.AddSingleton<ILogger, PlannerLogger>();
            services.AddSingleton(messageBroker);
            services.AddSingleton(typeof(ILogger<>), typeof(PlannerLogger<>));
            registerServicesAction.Invoke(Configuration, services, context);
            _provider = services.BuildServiceProvider();
        }

        private void InitializeMessageBroker(object messageBroker)
        {
            if (messageBroker == null)
            {
                throw new ApplicationException("MessageBroker at BaseJob.Execute(string, ref object) is null");
            }

            try
            {
                _messageBroker = new MessageBroker(messageBroker);

                var options = new JsonSerializerOptions
                {
                    Converters =
                    {
                        new TypeMappingConverter<IJobDetail, JobDetail>(),
                        new TypeMappingConverter<ITriggerDetail, TriggerDetail>(),
                        new TypeMappingConverter<IKey, Key>()
                    }
                };
                var ctx = JsonSerializer.Deserialize<JobExecutionContext>(_messageBroker.Details, options);
                if (ctx == null)
                {
                    throw new PlanarJobException("Fail to initialize JobExecutionContext from message broker detials (error 7379)");
                }

                FilterJobData(ctx.MergedJobDataMap);
                FilterJobData(ctx.JobDetails.JobDataMap);
                FilterJobData(ctx.TriggerDetails.TriggerDataMap);

                _context = ctx;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Fail to deserialize job execution context at BaseJob.Execute(string, ref object)", ex);
            }
        }

        private static void FilterJobData(SortedDictionary<string, string?> dictionary)
        {
            foreach (var item in Consts.AllDataKeys)
            {
                if (dictionary.ContainsKey(item))
                {
                    dictionary.Remove(item);
                }
            }
        }
    }
}