﻿using Microsoft.Extensions.Logging;
using Planar;
using Planar.Common;
using Planar.Common.API.Helpers;
using Planar.Job;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using IJobExecutionContext = Quartz.IJobExecutionContext;

namespace CommonJob
{
    public abstract class BaseCommonJob
    {
        protected static readonly string? IgnoreDataMapAttribute = typeof(IgnoreDataMapAttribute).FullName;
        protected static readonly string? JobDataMapAttribute = typeof(JobDataAttribute).FullName;
        protected static readonly string? TriggerDataMapAttribute = typeof(TriggerDataAttribute).FullName;

        protected static void DoNothingMethod()
        {
            //// *** Do Nothing Method *** ////
        }
    }

    public abstract class BaseCommonJob<TInstance, TProperties> : BaseCommonJob, IJob
    where TInstance : class
    where TProperties : class, new()
    {
        protected readonly ILogger<TInstance> _logger;
        private readonly IJobPropertyDataLayer _dataLayer;
        private JobMessageBroker? _messageBroker;

        protected BaseCommonJob(ILogger<TInstance> logger, IJobPropertyDataLayer dataLayer)
        {
            _logger = logger;
            _dataLayer = dataLayer;
        }

        public JobMessageBroker MessageBroker
        {
            get
            {
                if (_messageBroker == null) { throw new ArgumentNullException(nameof(MessageBroker)); }
                return _messageBroker;
            }
        }

        public TProperties Properties { get; private set; } = new();

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
                throw;
            }
        }

        protected async Task Initialize(IJobExecutionContext context)
        {
            await SetProperties(context);

            string? path = null;
            if (Properties is IPathJobProperties pathProperties)
            {
                path = pathProperties.Path;
            }

            var settings = LoadJobSettings(path);
            _messageBroker = new JobMessageBroker(context, settings);
        }

        protected IDictionary<string, string?> LoadJobSettings(string? path)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return new Dictionary<string, string?>();
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

        protected static int GetTimeout(TimeSpan? specificTimeout = null)
        {
            if (specificTimeout.HasValue && specificTimeout != TimeSpan.Zero)
            {
                return Convert.ToInt32(specificTimeout.Value.TotalMilliseconds);
            }

            return Convert.ToInt32(AppSettings.JobAutoStopSpan.TotalMilliseconds);
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

        protected void MapJobInstancePropertiesBack(IJobExecutionContext context, Type? targetType, object? instance)
        {
            //// ***** Attention: be aware for sync code with MapJobInstancePropertiesBack on Planar.Job.Test *****

            try
            {
                if (context == null) { return; }
                if (targetType == null) { return; }
                if (instance == null) { return; }

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

        protected void ValidateMandatoryString(string? value, string propertyName)
        {
            if (!string.IsNullOrEmpty(value)) { value = value.Trim(); }
            if (string.IsNullOrEmpty(value))
            {
                throw new PlanarJobException($"property '{propertyName}' is mandatory for job '{GetType().FullName}'");
            }
        }

        private bool IsIgnoreProperty(PropertyInfo property, JobKey jobKey, KeyValuePair<string, object> data)
        {
            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****

            var attributes = property.GetCustomAttributes();
            var ignore = attributes.Any(a => a.GetType().FullName == IgnoreDataMapAttribute);

            if (ignore)
            {
                _logger.LogDebug("Ignore map data key '{DataKey}' with value '{DataValue}' to property '{PropertyName}' of job '{JobGroup}.{JobName}'",
                    data.Key,
                    data.Value,
                    property.Name,
                    jobKey.Group,
                    jobKey.Name);
            }

            return ignore;

            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****
        }

        private bool IsIgnoreProperty(IEnumerable<Attribute> attributes, PropertyInfo property, JobKey jobKey)
        {
            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****

            var ignore = attributes.Any(a => a.GetType().FullName == IgnoreDataMapAttribute);

            if (ignore)
            {
                _logger.LogDebug("Ignore map back property '{PropertyName}' of job '{JobGroup}.{JobName}' to data map",
                    property.Name,
                    jobKey.Group,
                    jobKey.Name);
            }

            return ignore;

            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****
        }

        private void MapProperty(JobKey jobKey, object instance, PropertyInfo? prop, KeyValuePair<string, object> data)
        {
            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****

            if (prop == null) { return; }

            try
            {
                var ignore = IsIgnoreProperty(prop, jobKey, data);
                if (ignore) { return; }

                var underlyingType = Nullable.GetUnderlyingType(prop.PropertyType);
                var finalType = underlyingType ?? prop.PropertyType;

                // nullable property with null value in data
                if (underlyingType != null && string.IsNullOrEmpty(PlanarConvert.ToString(data.Value))) { return; }

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

        private void SafePutData(IJobExecutionContext context, object instance, PropertyInfo prop)
        {
            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****

            var attributes = prop.GetCustomAttributes();
            var ignore = IsIgnoreProperty(attributes, prop, context.JobDetail.Key);
            if (ignore) { return; }
            var jobData = attributes.Any(a => a.GetType().FullName == JobDataMapAttribute);
            var triggerData = attributes.Any(a => a.GetType().FullName == JobDataMapAttribute);

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

            string? value = null;
            try
            {
                if (!Consts.IsDataKeyValid(prop.Name))
                {
                    throw new PlanarJobException($"the data key {prop.Name} in invalid");
                }

                value = PlanarConvert.ToString(prop.GetValue(instance));
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

            string? value = null;
            try
            {
                if (!Consts.IsDataKeyValid(prop.Name))
                {
                    throw new PlanarJobException($"the data key {prop.Name} in invalid");
                }

                value = PlanarConvert.ToString(prop.GetValue(instance));
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

        private async Task SetProperties(IJobExecutionContext context)
        {
            var jobId = JobIdHelper.GetJobId(context.JobDetail);
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
    }
}