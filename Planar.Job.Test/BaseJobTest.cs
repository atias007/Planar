using CommonJob;
using Planar.API.Common.Entities;
using Planar.Common;
using Quartz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace Planar.Job.Test
{
    public abstract class BaseJobTest
    {
        private static Dictionary<string, string> LoadJobSettings<T>()
        {
            var path = new FileInfo(typeof(T).Assembly.Location).DirectoryName;
            var result = CommonUtil.LoadJobSettings(path);
            return result;
        }

        protected static JobInstanceLog ExecuteJob<T>(Dictionary<string, object> dataMap = null, DateTime? overrideNow = null)
        {
            Global.Environment = "UnitTest";
            var context = new MockJobExecutionContext(dataMap, overrideNow);
            var type = typeof(T);
            Validate(type);
            var method = type.GetMethod("Execute", BindingFlags.NonPublic | BindingFlags.Instance);
            var instance = Activator.CreateInstance<T>();
            MapJobInstanceProperties(context, instance);
            var settings = LoadJobSettings<T>();

            Exception jobException = null;
            var start = DateTime.Now;
            JobMessageBroker _broker;

            try
            {
                _broker = new JobMessageBroker(context, settings);
                var result = method.Invoke(instance, new object[] { _broker }) as Task;
                result.ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                jobException = ex;
            }
            finally
            {
                context.JobRunTime = DateTime.Now.Subtract(start);
            }

            var data = context.MergedJobDataMap.Keys.Count == 0 ? null : JsonSerializer.Serialize(context.MergedJobDataMap);
            var duration = context.JobRunTime.TotalMilliseconds;
            var endDate = context.FireTimeUtc.DateTime.Add(context.JobRunTime);
            var status = jobException == null ? 0 : 1;

            var metadata = context.Result as JobExecutionMetadata;

            var log = new JobInstanceLog
            {
                InstanceId = context.FireInstanceId,
                Data = data,
                StartDate = context.FireTimeUtc.DateTime,
                JobName = context.JobDetail.Key.Name,
                JobGroup = context.JobDetail.Key.Group,
                JobId = context.JobDetail.JobDataMap.GetString(Consts.JobId),
                TriggerName = context.Trigger.Key.Name,
                TriggerGroup = context.Trigger.Key.Group,
                TriggerId = context.Trigger.JobDataMap.GetString(Consts.TriggerId),
                Duration = Convert.ToInt32(duration),
                EndDate = endDate,
                Exception = jobException?.ToString(),
                EffectedRows = metadata?.EffectedRows,
                Information = metadata?.GetInformation(),
                Id = -1,
                IsStopped = false,
                Retry = false,
                StatusTitle = ((StatusMembers)status).ToString(),
                Status = status
            };

            return log;
        }

        private static void MapJobInstanceProperties<T>(IJobExecutionContext context, T instance)
        {
            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on BaseCommonJob *****

            var propInfo = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
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

            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on BaseCommonJob *****
        }

        private static MethodInfo Validate(Type type)
        {
            //// ***** Attention: be aware for sync code with Validate on BaseCommonJob *****

            var method = type.GetMethod("Execute", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null)
            {
                throw new ApplicationException($"Type '{type.Name}' has no 'Execute' method");
            }

            if (method.ReturnType != typeof(Task))
            {
                throw new ApplicationException($"Method 'Execute' at type '{type.Name}' has no 'Task' return type (current return type is {method.ReturnType.FullName})");
            }

            var parameters = method.GetParameters();
            if (parameters?.Length != 1)
            {
                throw new ApplicationException($"Method 'Execute' at type '{type.Name}' must have only 1 parameters (current parameters count {parameters?.Length})");
            }

            if (parameters[0].ParameterType.ToString().StartsWith("System.Object") == false)
            {
                throw new ApplicationException($"Second parameter in method 'Execute' at type '{type.Name}' must be object. (current type '{parameters[1].ParameterType.Name}')");
            }

            return method;

            //// ***** Attention: be aware for sync code with Validate on BaseCommonJob *****
        }
    }
}