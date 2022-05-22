using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job.Logger;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Planar.Job
{
    public abstract class BaseJob
    {
        private JobExecutionContext _context;
        private bool? _isNowOverrideValueExists;
        private MessageBroker _messageBroker;
        private DateTime? _nowOverrideValue;
        private IServiceProvider _provider;

        protected IConfiguration Configuration
        {
            get
            {
                return _provider.GetRequiredService<IConfiguration>();
            }
        }

        protected ILogger Logger { get; private set; }

        protected IServiceProvider ServiceProvider
        {
            get
            {
                return _provider;
            }
        }

        public abstract void Configure(IConfigurationBuilder configurationBuilder);

        public abstract Task ExecuteJob(IJobExecutionContext context);

        public abstract void RegisterServices(IServiceCollection services);

        internal Task Execute(ref object messageBroker)
        {
            InitializeMessageBroker(messageBroker);
            InitializeDepedencyInjection(_context, _messageBroker);

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
            _messageBroker.Publish(MessageBrokerChannels.AddAggragateException, message);
        }

        protected void CheckAggragateException()
        {
            var text = _messageBroker.Publish(MessageBrokerChannels.GetExceptionsText);
            if (string.IsNullOrEmpty(text) == false)
            {
                var ex = new PlanarJobAggragateException(text);
                throw ex;
            }
        }

        protected bool CheckIfStopRequest()
        {
            var text = _messageBroker.Publish(MessageBrokerChannels.CheckIfStopRequest);
            _ = bool.TryParse(text, out var stop);
            return stop;
        }

        protected void FailOnStopRequest(Action stopHandle = default)
        {
            if (stopHandle != default)
            {
                stopHandle.Invoke();
            }

            _messageBroker.Publish(MessageBrokerChannels.FailOnStopRequest);
        }

        protected T GetData<T>(string key)
        {
            var value = _messageBroker.Publish(MessageBrokerChannels.GetData, key);
            var result = (T)Convert.ChangeType(value, typeof(T));
            return result;
        }

        protected string GetData(string key)
        {
            return GetData<string>(key);
        }

        protected int? GetEffectedRows()
        {
            var text = _messageBroker.Publish(MessageBrokerChannels.GetEffectedRows);
            _ = int.TryParse(text, out var rows);
            return rows;
        }

        protected void IncreaseEffectedRows(int delta = 1)
        {
            _messageBroker.Publish(MessageBrokerChannels.IncreaseEffectedRows, delta);
        }

        protected bool IsDataExists(string key)
        {
            var text = _messageBroker.Publish(MessageBrokerChannels.DataContainsKey, key);
            _ = bool.TryParse(text, out var result);
            return result;
        }

        protected TimeSpan JobRunTime
        {
            get
            {
                var text = _messageBroker.Publish(MessageBrokerChannels.JobRunTime);
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
                var value = GetData(Consts.NowOverrideValue);
                if (DateTime.TryParse(value, out DateTime dateValue))
                {
                    _nowOverrideValue = dateValue;
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
            _messageBroker.Publish(MessageBrokerChannels.PutJobData, message);
        }

        protected void PutTriggerData(string key, object value)
        {
            var message = new { Key = key, Value = value };
            _messageBroker.Publish(MessageBrokerChannels.PutTriggerData, message);
        }

        protected void SetEffectedRows(int value)
        {
            _messageBroker.Publish(MessageBrokerChannels.SetEffectedRows, value);
        }

        protected void UpdateProgress(byte value)
        {
            if (value > 100) { value = 100; }
            if (value < 0) { value = 0; }
            _messageBroker.Publish(MessageBrokerChannels.UpdateProgress, value);
        }

        protected void UpdateProgress(int current, int total)
        {
            var percentage = 1.0 * current / total;
            var value = Convert.ToByte(percentage * 100);
            UpdateProgress(value);
        }

        private IConfiguration GetConfiguration(JobExecutionContext context)
        {
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(context.JobSettings);
            Configure(builder);
            var result = builder.Build();
            return result;
        }

        private void InitializeDepedencyInjection(JobExecutionContext context, MessageBroker messageBroker)
        {
            var services = new ServiceCollection();
            var configuration = GetConfiguration(context);
            services.AddSingleton(configuration);
            services.AddSingleton<IJobExecutionContext>(context);
            services.AddSingleton(messageBroker);
            services.AddSingleton<ILogger, PlannerLogger>();
            RegisterServices(services);
            _provider = services.BuildServiceProvider();
        }

        private void InitializeMessageBroker(object messageBroker)
        {
            if (messageBroker == null)
            {
                throw new ApplicationException("MessageBroker at BaseJob.Execute(string, ref object) is null");
            }

            _messageBroker = new MessageBroker(messageBroker);

            try
            {
                var options = new JsonSerializerOptions
                {
                    Converters =
                    {
                        new TypeMappingConverter<IJobDetail, JobDetail>(),
                        new TypeMappingConverter<ITriggerDetail, TriggerDetail>(),
                        new TypeMappingConverter<IKey, Key>()
                    }
                };
                _context = JsonSerializer.Deserialize<JobExecutionContext>(_messageBroker.Details, options);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Fail to deserialize job execution context at BaseJob.Execute(string, ref object)", ex);
            }

            _messageBroker = new MessageBroker(messageBroker);
        }
    }
}