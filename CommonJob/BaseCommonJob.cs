using Microsoft.Extensions.Logging;
using Planar;
using Planar.Common;
using Quartz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace CommonJob
{
    [PersistJobDataAfterExecution]
    public abstract class BaseCommonJob<TInstance> : IJob, ICommonJob
        where TInstance : class, new()
    {
        public string JobPath { get; set; }

        private readonly Dictionary<string, string> _jobRunningProperties = new();

        public void SetJobRunningProperty<TPropery>(string key, TPropery value)
        {
            var json = JsonSerializer.Serialize(value);
            _jobRunningProperties.Add(key, json);
        }

        public JobExecutionException GetJobExecutingException(JobExecutionException ex, IJobExecutionContext context)
        {
            var message = $"FireInstanceId {context.FireInstanceId} throw JobExecutionException with message {ex.Message}";
            var jobException = new JobExecutionException(message, ex)
            {
                RefireImmediately = ex.RefireImmediately,
                Source = ex.Source,
                UnscheduleAllTriggers = ex.UnscheduleAllTriggers,
                UnscheduleFiringTrigger = ex.UnscheduleFiringTrigger,
            };

            return jobException;
        }

        public bool ContainsJobRunningProperty(string key)
        {
            var result = _jobRunningProperties.ContainsKey(key);
            return result;
        }

        public TPropery GetJobRunningProperty<TPropery>(string key)
        {
            if (_jobRunningProperties.ContainsKey(key))
            {
                var json = _jobRunningProperties[key];
                var value = JsonSerializer.Deserialize<TPropery>(json);
                return value;
            }
            else
            {
                return default;
            }
        }

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
                    if (item.Key.StartsWith("__") == false)
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
                // Load job global parameters
                var final = Global.Parameters;

                // Merge settings yml file
                if (string.IsNullOrEmpty(JobPath)) return null;
                var fullpath = Path.Combine(FolderConsts.BasePath, FolderConsts.Data, FolderConsts.Jobs, JobPath);
                var location = new DirectoryInfo(fullpath);
                var jobSettings = CommonUtil.LoadJobSettings(location.FullName);
                final = final.Merge(jobSettings);

                foreach (var item in jobSettings)
                {
                    if (final.ContainsKey(item.Key))
                    {
                        final[item.Key] = item.Value;
                    }
                    else
                    {
                        final.Add(item.Key, item.Value);
                    }
                }

                return final;
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
            if (string.IsNullOrEmpty(value) == false) { value = value.Trim(); }
            if (string.IsNullOrEmpty(value))
            {
                throw new ApplicationException($"Property '{propertyName}' is mandatory for job '{GetType().FullName}'");
            }
        }

        protected virtual void Validate()
        {
            ValidateMandatoryString(JobPath, nameof(JobPath));
        }
    }
}