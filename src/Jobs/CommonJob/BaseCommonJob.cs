using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Common.Exceptions;
using Planar.Common.Helpers;
using Planar.Service.API.Helpers;
using Quartz;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using IJobExecutionContext = Quartz.IJobExecutionContext;

namespace CommonJob
{
    public abstract class BaseCommonJob
    {
        private JobMessageBroker _messageBroker = null!;

        protected IDictionary<string, string?> Settings { get; private set; } = new Dictionary<string, string?>();

        protected JobMessageBroker MessageBroker => _messageBroker;

        protected static void DoNothingMethod()
        {
            //// *** Do Nothing Method *** ////
        }

        protected static void HandleException(IJobExecutionContext context, Exception ex)
        {
            var metadata = JobExecutionMetadata.GetInstance(context);
            if (ex is TargetInvocationException)
            {
                metadata.UnhandleException = ex.InnerException;
            }
            else
            {
                metadata.UnhandleException = ex;
            }
        }

        protected async Task WaitForJobTask(IJobExecutionContext context, Task task)
        {
            var timeout = TriggerHelper.GetTimeoutWithDefault(context.Trigger);
            var finish = task.Wait(timeout);
            if (!finish)
            {
                MessageBroker.AppendLog(LogLevel.Warning, $"Timeout occur, sent cancel requst to job (timeout value: {FormatTimeSpan(timeout)})");
                await context.Scheduler.Interrupt(context.JobDetail.Key);
            }

            task.Wait();
        }

        internal void FillSettings(IDictionary<string, string?> settings)
        {
            Settings = settings;
        }

        internal void SetMessageBroker(JobMessageBroker messageBroker)
        {
            _messageBroker = messageBroker;
        }

        private static string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalSeconds < 1) { return $"{timeSpan.TotalMilliseconds:N0}ms"; }
            if (timeSpan.TotalDays >= 1) { return $"{timeSpan:\\(d\\)\\ hh\\:mm\\:ss}"; }
            return $"{timeSpan:hh\\:mm\\:ss}";
        }
    }

    public abstract class BaseCommonJob<TInstance, TProperties> : BaseCommonJob, IJob
    where TInstance : class
    where TProperties : class, new()
    {
        protected readonly ILogger<TInstance> _logger;
        private readonly IJobPropertyDataLayer _dataLayer;

        protected BaseCommonJob(ILogger<TInstance> logger, IJobPropertyDataLayer dataLayer)
        {
            _logger = logger;
            _dataLayer = dataLayer;
        }

        public TProperties Properties { get; private set; } = new();

        public string FireInstanceId { get; private set; } = string.Empty;

        public abstract Task Execute(IJobExecutionContext context);

        protected void FinalizeJob(IJobExecutionContext context)
        {
            try
            {
                var metadata = JobExecutionMetadata.GetInstance(context);
                metadata.Progress = 100;
            }
            catch (Exception ex)
            {
                var source = nameof(FinalizeJob);
                _logger.LogError(ex, "Fail at {Source} with job {Group}.{Name}", source, context.JobDetail.Key.Group, context.JobDetail.Key.Name);
            }
        }

        protected async Task Initialize(IJobExecutionContext context, IMonitorUtil? monitorUtil = null)
        {
            await SetProperties(context);

            string? path = null;
            if (Properties is IPathJobProperties pathProperties)
            {
                path = pathProperties.Path;
            }

            FillSettings(LoadJobSettings(path));
            SetMessageBroker(new JobMessageBroker(context, Settings, monitorUtil));

            context.CancellationToken.Register(() =>
            {
                MessageBroker.AppendLog(LogLevel.Warning, "Service get a request for cancel job");
            });
        }

        protected IDictionary<string, string?> LoadJobSettings(string? path)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return new Dictionary<string, string?>();
                var jobSettings = JobSettingsLoader.LoadJobSettings(path, Global.GlobalConfig);
                return jobSettings;
            }
            catch (Exception ex)
            {
                var source = nameof(LoadJobSettings);
                _logger.LogError(ex, "Fail at {Source}", source);
                throw;
            }
        }

        protected void ValidateMandatoryString(string? value, string propertyName)
        {
            if (!string.IsNullOrEmpty(value)) { value = value.Trim(); }
            if (string.IsNullOrEmpty(value))
            {
                throw new PlanarException($"property {propertyName} is mandatory for job '{GetType().FullName}'");
            }
        }

        private async Task SetProperties(IJobExecutionContext context)
        {
            var jobId = JobHelper.GetJobId(context.JobDetail);
            if (jobId == null)
            {
                var title = JobHelper.GetKeyTitle(context.JobDetail);
                throw new PlanarException($"fail to get job id while execute job {title}");
            }

            var properties = await _dataLayer.GetJobProperty(jobId);
            if (string.IsNullOrEmpty(properties))
            {
                var title = JobHelper.GetKeyTitle(context.JobDetail);
                throw new PlanarException($"fail to get job properties while execute job {title} (id: {jobId})");
            }

            Properties = YmlUtil.Deserialize<TProperties>(properties);
            FireInstanceId = context.FireInstanceId;
        }
    }
}