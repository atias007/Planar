using Microsoft.Extensions.Logging;
using Planar;
using Planar.Common;
using Planar.Job;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using IJobDetail = Quartz.IJobDetail;
using IJobExecutionContext = Quartz.IJobExecutionContext;

namespace CommonJob
{
    public abstract class BaseCommonJob<TInstance, TProperties> : IJob
    where TInstance : class
    where TProperties : class, new()
    {
        protected readonly ILogger<TInstance> _logger;
        private readonly IJobPropertyDataLayer _dataLayer;
        private JobMessageBroker _messageBroker;
        private static readonly string _ignoreDataMapAttribute = typeof(IgnoreDataMapAttribute).FullName;
        private static readonly string _jobDataMapAttribute = typeof(JobDataAttribute).FullName;
        private static readonly string _triggerDataMapAttribute = typeof(TriggerDataAttribute).FullName;

        protected BaseCommonJob(ILogger<TInstance> logger, IJobPropertyDataLayer dataLayer)
        {
            _logger = logger;
            _dataLayer = dataLayer;
        }

        public TProperties Properties { get; private set; } = new();

        public abstract Task Execute(IJobExecutionContext context);

        public JobMessageBroker MessageBroker => _messageBroker;

        private async Task SetProperties(IJobExecutionContext context)
        {
            var jobId = GetJobId(context.JobDetail);
            if (jobId == null)
            {
                var key = context.JobDetail.Key;
                throw new PlanarJobException($"fail to get job id while execute job {key.Group}.{key.Name}");
            }

            var properties = await _dataLayer.GetJobProperty(jobId);
            if (string.IsNullOrEmpty(properties))
            {
                var key = context.JobDetail.Key;
                throw new PlanarJobException($"fail to get job properties while execute job {key.Group}.{key.Name} (id: {jobId})");
            }

            Properties = YmlUtil.Deserialize<TProperties>(properties);
        }

        protected async Task Initialize(IJobExecutionContext context)
        {
            await SetProperties(context);

            string path = null;
            if (Properties is IPathJobProperties pathProperties)
            {
                path = pathProperties.Path;
            }

            var settings = LoadJobSettings(path);
            _messageBroker = new JobMessageBroker(context, settings);
        }

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
                throw;
            }
        }

        protected void MapJobInstanceProperties(IJobExecutionContext context, Type targetType, object instance)
        {
            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****

            try
            {
                var allProperties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
                foreach (var item in context.MergedJobDataMap)
                {
                    if (item.Key.StartsWith(Consts.ConstPrefix)) { continue; }
                    var prop = allProperties.FirstOrDefault(p => string.Equals(p.Name, item.Key, StringComparison.OrdinalIgnoreCase));
                    MapProperty(context.JobDetail.Key, instance, prop, item);
                }
            }
            catch (Exception ex)
            {
                var source = nameof(MapJobInstanceProperties);
                _logger.LogError(ex, "Fail at {Source} with job {Group}.{Name}", source, context.JobDetail.Key.Group, context.JobDetail.Key.Name);
                throw;
            }

            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****
        }

        private void MapProperty(JobKey jobKey, object instance, PropertyInfo prop, KeyValuePair<string, object> data)
        {
            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****

            if (prop == null) { return; }

            try
            {
                var attributes = prop.GetCustomAttributes();
                var ignore = attributes.Any(a => a.GetType().FullName == _ignoreDataMapAttribute);

                if (ignore)
                {
                    _logger.LogDebug(
                        "Ignore map data key '{Key}' with value {Value} to property {Name} of job {JobGroup}.{JobName}",
                        data.Key, data.Value, prop.Name, jobKey.Group, jobKey.Name);

                    return;
                }

                var underlyingType = Nullable.GetUnderlyingType(prop.PropertyType);
                var finalType = underlyingType ?? prop.PropertyType;

                // nullable property with null value in data
                if (underlyingType != null && string.IsNullOrEmpty(Convert.ToString(data.Value))) { return; }

                var value = Convert.ChangeType(data.Value, finalType);
                prop.SetValue(instance, value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Fail to map data key '{Key}' with value {Value} to property {Name} of job {JobGroup}.{JobName}",
                    data.Key, data.Value, prop.Name, jobKey.Group, jobKey.Name);
            }

            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****
        }

        protected void MapJobInstancePropertiesBack(IJobExecutionContext context, Type targetType, object instance)
        {
            //// ***** Attention: be aware for sync code with MapJobInstancePropertiesBack on Planar.Job.Test *****

            try
            {
                var propInfo = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
                foreach (var prop in propInfo)
                {
                    if (prop.Name.StartsWith(Consts.ConstPrefix)) { continue; }
                    SafePutData(context, instance, prop);
                }
            }
            catch (Exception ex)
            {
                var source = nameof(MapJobInstancePropertiesBack);
                _logger.LogError(ex, "Fail at {Source} with job {Group}.{Name}", source, context.JobDetail.Key.Group, context.JobDetail.Key.Name);
                throw;
            }

            //// ***** Attention: be aware for sync code with MapJobInstancePropertiesBack on Planar.Job.Test *****
        }

        private void SafePutData(IJobExecutionContext context, object instance, PropertyInfo prop)
        {
            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****

            var attributes = prop.GetCustomAttributes();
            var jobData = attributes.Any(a => a.GetType().FullName == _jobDataMapAttribute);
            var triggerData = attributes.Any(a => a.GetType().FullName == _jobDataMapAttribute);

            if (jobData)
            {
                SafePutJobDataMap(context, instance, prop);
            }

            if (triggerData)
            {
                SafePutTiggerDataMap(context, instance, prop);
            }

            if (!jobData && !triggerData)
            {
                if (context.JobDetail.JobDataMap.ContainsKey(prop.Name))
                {
                    SafePutJobDataMap(context, instance, prop);
                }

                if (context.Trigger.JobDataMap.ContainsKey(prop.Name))
                {
                    SafePutTiggerDataMap(context, instance, prop);
                }
            }

            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****
        }

        private void SafePutJobDataMap(IJobExecutionContext context, object instance, PropertyInfo prop)
        {
            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****

            string value = null;
            try
            {
                if (!Consts.IsDataKeyValid(prop.Name))
                {
                    throw new PlanarJobException($"the data key {prop.Name} in invalid");
                }

                value = Convert.ToString(prop.GetValue(instance));
                context.JobDetail.JobDataMap.Put(prop.Name, value);
            }
            catch (Exception ex)
            {
                var jobKey = context.JobDetail.Key;
                _logger.LogWarning(ex,
                    "Fail to save back value {Value} from property {Name} to JobDetails at job {JobGroup}.{JobName}",
                    value, prop.Name, jobKey.Group, jobKey.Name);
            }

            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****
        }

        private void SafePutTiggerDataMap(IJobExecutionContext context, object instance, PropertyInfo prop)
        {
            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****

            string value = null;
            try
            {
                if (!Consts.IsDataKeyValid(prop.Name))
                {
                    throw new PlanarJobException($"the data key {prop.Name} in invalid");
                }

                value = Convert.ToString(prop.GetValue(instance));
                context.Trigger.JobDataMap.Put(prop.Name, value);
            }
            catch (Exception ex)
            {
                var jobKey = context.JobDetail.Key;
                _logger.LogWarning(ex,
                    "Fail to save back value {Value} from property {Name} to TriggerDetails at job {JobGroup}.{JobName}",
                    value, prop.Name, jobKey.Group, jobKey.Name);
            }

            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****
        }

        protected Dictionary<string, string> LoadJobSettings(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return new Dictionary<string, string>();
                var jobSettings = JobSettingsLoader.LoadJobSettings(path);
                return jobSettings;
            }
            catch (Exception ex)
            {
                var source = nameof(LoadJobSettings);
                _logger.LogError(ex, "Fail at {Source}", source);
                throw;
            }
        }

        protected void ValidateMandatoryString(string value, string propertyName)
        {
            if (!string.IsNullOrEmpty(value)) { value = value.Trim(); }
            if (string.IsNullOrEmpty(value))
            {
                throw new PlanarJobException($"property '{propertyName}' is mandatory for job '{GetType().FullName}'");
            }
        }

        private static string GetJobId(IJobDetail job)
        {
            if (job == null)
            {
                throw new PlanarJobException("job is null at JobKeyHelper.GetJobId(IJobDetail)");
            }

            if (job.JobDataMap.TryGetValue(Consts.JobId, out var id))
            {
                return Convert.ToString(id);
            }

            return null;
        }
    }
}