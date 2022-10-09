using Microsoft.Extensions.Logging;
using Planar;
using Planar.Common;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace CommonJob
{
    public abstract class BaseCommonJob<TInstance> : IJob
        where TInstance : class, new()
    {
        public string JobPath { get; set; }

        public abstract Task Execute(IJobExecutionContext context);

        protected LazySingleton<ILogger<TInstance>> Logger = new(Global.GetLogger<TInstance>);

        protected void MapProperties(IJobExecutionContext context)
        {
            //// ***** Attention: be aware for sync code with MapProperties on BaseJobTest *****
            try
            {
                var json = context.JobDetail.JobDataMap[Consts.JobTypeProperties] as string;
                if (string.IsNullOrEmpty(json)) return;
                var list = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (list == null) return;
                var propInfo = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
                foreach (var item in list)
                {
                    var p = propInfo.FirstOrDefault(p => string.Compare(p.Name, item.Key, true) == 0);
                    if (p != null)
                    {
                        var value = Convert.ChangeType(item.Value, p.PropertyType);
                        p.SetValue(this, value);
                    }
                }
            }
            catch (Exception ex)
            {
                var source = nameof(MapProperties);
                Logger.Instance.LogError(ex, "Fail at {Source} with job {Group}.{Name}", source, context.JobDetail.Key.Group, context.JobDetail.Key.Name);
                throw;
            }

            //// ***** Attention: be aware for sync code with MapProperties on BaseJobTest *****
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
                Logger.Instance.LogError(ex, "Fail at {Source} with job {Group}.{Name}", source, context.JobDetail.Key.Group, context.JobDetail.Key.Name);
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
                Logger.Instance.LogError(ex, "Fail at {Source} with job {Group}.{Name}", source, context.JobDetail.Key.Group, context.JobDetail.Key.Name);
                throw;
            }
        }

        protected Dictionary<string, string> LoadJobSettings()
        {
            try
            {
                if (string.IsNullOrEmpty(JobPath)) return new Dictionary<string, string>();
                var jobSettings = JobSettingsLoader.LoadJobSettings(JobPath);
                return jobSettings;
            }
            catch (Exception ex)
            {
                var source = nameof(LoadJobSettings);
                Logger.Instance.LogError(ex, "Fail at {Source}", source);
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

        protected virtual void Validate()
        {
            ValidateMandatoryString(JobPath, nameof(JobPath));
        }
    }
}