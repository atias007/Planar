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

        private IServiceProvider _provider = null!;
        private IConfiguration _configuration = null!;
        private ILogger _logger = null!;
        private BaseJobFactory _baseJobFactory = null!;

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

        internal Task Execute(ref object messageBroker)
        {
            Action<IConfigurationBuilder, IJobExecutionContext> configureAction = Configure;
            Action<IConfiguration, IServiceCollection, IJobExecutionContext> registerServicesAction = RegisterServices;

            InitializeBaseJobFactory(messageBroker);
            InitializeConfiguration(_context, configureAction);
            InitializeDepedencyInjection(_context, _baseJobFactory, registerServicesAction);

            Logger = ServiceProvider.GetRequiredService<ILogger>();

            return ExecuteJob(_context)
                .ContinueWith(HandleTaskContinue);
        }

        internal Task ExecuteUnitTest(
            ref object messageBroker,
            Action<IConfigurationBuilder, IJobExecutionContext> configureAction,
            Action<IConfiguration, IServiceCollection, IJobExecutionContext> registerServicesAction)

        {
            InitializeBaseJobFactory(messageBroker);
            InitializeConfiguration(_context, configureAction);
            InitializeDepedencyInjection(_context, _baseJobFactory, registerServicesAction);

            Logger = ServiceProvider.GetRequiredService<ILogger>();

            return ExecuteJob(_context)
                .ContinueWith(HandleTaskContinue);
        }

        private static void HandleTaskContinue(Task task)
        {
            if (task.Exception != null) { throw task.Exception; }
            if (task.Exception == null && task.Status == TaskStatus.Canceled)
            {
                throw new TaskCanceledException("Request for cancel job");
            }
        }

        [Obsolete("AddAggragateException is deprecated because spelling error. Use AddAggregateException instead")]
        protected void AddAggragateException(Exception ex)
        {
            AddAggregateException(ex);
        }

        protected void AddAggregateException(Exception ex)
        {
            _baseJobFactory.AddAggregateException(ex);
        }

        protected void CheckAggragateException()
        {
            _baseJobFactory.CheckAggragateException();
        }

        [Obsolete("CheckIfStopRequest is no longer supported. Use cancellation token in IJobExecutionContext")]
        protected bool CheckIfStopRequest()
        {
            return _baseJobFactory.CheckIfStopRequest();
        }

        [Obsolete("FailOnStopRequest is no longer supported. Use cancellation token in IJobExecutionContext")]
        protected void FailOnStopRequest(Action? stopHandle = default)
        {
            _baseJobFactory.FailOnStopRequest(stopHandle);
        }

        protected T GetData<T>(string key)
        {
            return _baseJobFactory.GetData<T>(key);
        }

        protected string GetData(string key)
        {
            return _baseJobFactory.GetData(key);
        }

        protected int? GetEffectedRows()
        {
            return _baseJobFactory.GetEffectedRows();
        }

        protected void IncreaseEffectedRows(int delta = 1)
        {
            _baseJobFactory.IncreaseEffectedRows(delta);
        }

        protected bool IsDataExists(string key)
        {
            return _baseJobFactory.IsDataExists(key);
        }

        protected TimeSpan JobRunTime => _baseJobFactory.JobRunTime;

        protected DateTime Now()
        {
            return _baseJobFactory.Now();
        }

        protected void PutJobData(string key, object value)
        {
            _baseJobFactory.PutJobData(key, value);
        }

        protected void PutTriggerData(string key, object value)
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
            services.AddSingleton(baseJobFactory.MessageBroker);
            services.AddSingleton(typeof(ILogger<>), typeof(PlanarLogger<>));
            registerServicesAction.Invoke(Configuration, services, context);
            _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        }

        private void InitializeBaseJobFactory(object messageBroker)
        {
            if (messageBroker == null)
            {
                throw new PlanarJobException("MessageBroker at BaseJob.Execute(string, ref object) is null");
            }

            try
            {
                var mb = new MessageBroker(messageBroker);
                _baseJobFactory = new BaseJobFactory(mb);

                var options = new JsonSerializerOptions
                {
                    Converters =
                    {
                        new TypeMappingConverter<IJobDetail, JobDetail>(),
                        new TypeMappingConverter<ITriggerDetail, TriggerDetail>(),
                        new TypeMappingConverter<IKey, Key>()
                    }
                };
                var ctx = JsonSerializer.Deserialize<JobExecutionContext>(mb.Details, options) ??
                    throw new PlanarJobException("Fail to initialize JobExecutionContext from message broker detials (error 7379)");

                ctx.CancellationToken = mb.CancellationToken;

                FilterJobData(ctx.MergedJobDataMap);
                FilterJobData(ctx.JobDetails.JobDataMap);
                FilterJobData(ctx.TriggerDetails.TriggerDataMap);

                _context = ctx;
            }
            catch (Exception ex)
            {
                throw new PlanarJobException("Fail to deserialize job execution context at BaseJob.Execute(string, ref object)", ex);
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