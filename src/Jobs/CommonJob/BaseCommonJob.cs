using Microsoft.Extensions.Logging;
using Planar;
using Planar.Common;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CommonJob
{
    public abstract class BaseCommonJob<TInstance, TProperties> : IJob
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

        public abstract Task Execute(IJobExecutionContext context);

        protected async Task SetProperties(IJobExecutionContext context)
        {
            var jobId = GetJobId(context.JobDetail);
            if (jobId == null)
            {
                var key = context.JobDetail.Key;
                throw new PlanarJobException($"Fail to get job id while execute job {key.Group}.{key.Name}");
            }

            var properties = await _dataLayer.GetJobProperty(jobId);
            if (string.IsNullOrEmpty(properties))
            {
                var key = context.JobDetail.Key;
                throw new PlanarJobException($"Fail to get job properties while execute job {key.Group}.{key.Name} (id: {jobId})");
            }

            Properties = YmlUtil.Deserialize<TProperties>(properties);
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
            try
            {
                var propInfo = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
                foreach (var item in context.MergedJobDataMap)
                {
                    if (!item.Key.StartsWith("__"))
                    {
                        var p = propInfo.FirstOrDefault(p => p.Name == item.Key);
                        if (p != null)
                        {
                            try
                            {
                                var value = Convert.ChangeType(item.Value, p.PropertyType);
                                p.SetValue(instance, value);
                            }
                            catch (Exception)
                            {
                                // *** DO NOTHING *** //
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var source = nameof(MapJobInstanceProperties);
                _logger.LogError(ex, "Fail at {Source} with job {Group}.{Name}", source, context.JobDetail.Key.Group, context.JobDetail.Key.Name);
                throw;
            }
        }

        protected void MapJobInstancePropertiesBack(IJobExecutionContext context, Type targetType, object instance)
        {
            try
            {
                var propInfo = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
                foreach (var p in propInfo)
                {
                    if (p.Name.StartsWith("__")) { continue; }
                    if (context.JobDetail.JobDataMap.ContainsKey(p.Name))
                    {
                        SafePutJobDataMap(context, instance, p);
                    }

                    if (context.Trigger.JobDataMap.ContainsKey(p.Name))
                    {
                        SafePutTiggerDataMap(context, instance, p);
                    }
                }
            }
            catch (Exception ex)
            {
                var source = nameof(MapJobInstancePropertiesBack);
                _logger.LogError(ex, "Fail at {Source} with job {Group}.{Name}", source, context.JobDetail.Key.Group, context.JobDetail.Key.Name);
                throw;
            }
        }

        private static void SafePutJobDataMap(IJobExecutionContext context, object instance, PropertyInfo p)
        {
            try
            {
                var value = Convert.ToString(p.GetValue(instance));
                context.JobDetail.JobDataMap.Put(p.Name, value);
            }
            catch
            {
                // *** DO NOTHING *** //
            }
        }

        private static void SafePutTiggerDataMap(IJobExecutionContext context, object instance, PropertyInfo p)
        {
            try
            {
                var value = Convert.ToString(p.GetValue(instance));
                context.Trigger.JobDataMap.Put(p.Name, value);
            }
            catch
            {
                // *** DO NOTHING *** //
            }
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
                throw new PlanarJobException($"Property '{propertyName}' is mandatory for job '{GetType().FullName}'");
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