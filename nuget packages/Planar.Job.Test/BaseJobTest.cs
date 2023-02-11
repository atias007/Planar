using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace Planar.Job.Test
{
    public abstract class BaseJobTest
    {
        private static readonly string _ignoreDataMapAttribute = typeof(IgnoreDataMapAttribute).FullName;

        protected abstract void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context);

        protected abstract void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context);

        protected IJobExecutionResult ExecuteJob<T>()
            where T : class, new()
        {
            var props = new ExecuteJobProperties
            {
                JobType = typeof(T)
            };

            return ExecuteJob(props);
        }

        protected IJobExecutionResult ExecuteJob(ExecuteJobBuilder builder)
        {
            var props = builder.Build();
            return ExecuteJob(props);
        }

        private static void MapJobInstanceProperties<T>(IJobExecutionContext context, T instance)
        {
            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on BaseCommonJob *****

            var allProperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
            foreach (var item in context.MergedJobDataMap)
            {
                if (item.Key.StartsWith(Consts.ConstPrefix)) { continue; }
                var prop = allProperties.FirstOrDefault(p => string.Equals(p.Name, item.Key, StringComparison.OrdinalIgnoreCase));
                MapProperty(context.JobDetails.Key, instance, prop, item);
            }

            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on BaseCommonJob *****
        }

        private static void MapProperty(IKey jobKey, object instance, PropertyInfo prop, KeyValuePair<string, string> data)
        {
            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on BaseCommonJob *****

            if (prop == null) { return; }

            try
            {
                var attributes = prop.GetCustomAttributes();
                var ignore = attributes.Any(a => a.GetType().FullName == _ignoreDataMapAttribute);

                if (ignore)
                {
                    Console.WriteLine($"Ignore map data key '{data.Key}' with value {data.Value} to property {prop.Name} of job {jobKey.Group}.{jobKey.Name}");
                    return;
                }

                var value = Convert.ChangeType(data.Value, prop.PropertyType);
                prop.SetValue(instance, value);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fail to map data key '{data.Key}' with value {data.Value} to property {prop.Name} of job {jobKey.Group}.{jobKey.Name}");
                Console.WriteLine(ex);
            }

            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on BaseCommonJob *****
        }

        private static MethodInfo ValidateBaseJob(Type type)
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

            if (!parameters[0].ParameterType.ToString().StartsWith("System.Object"))
            {
                throw new ApplicationException($"Second parameter in method 'Execute' at type '{type.Name}' must be object. (current type '{parameters[1].ParameterType.Name}')");
            }

            return method;

            //// ***** Attention: be aware for sync code with Validate on BaseCommonJob *****
        }

        private IJobExecutionResult ExecuteJob(ExecuteJobProperties properties)
        {
            var context = new MockJobExecutionContext(properties);
            ValidateBaseJob(properties.JobType);
            var method = properties.JobType.GetMethod("ExecuteUnitTest", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null)
            {
                throw new Exception($"Can not run test. Worker type {properties.JobType.FullName} does not inherit BaseJob / does not use Planar.Job nuget pack with version 1.0.4 or higher");
            }

            var instance = Activator.CreateInstance(properties.JobType);
            MapJobInstanceProperties(context, instance);
            var settings = JobSettingsLoader.LoadJobSettingsForUnitTest(properties.JobType);
            settings = settings.Merge(properties.GlobalSettings);

            Exception jobException = null;
            var start = DateTime.Now;
            JobMessageBroker _broker;

            try
            {
                _broker = new JobMessageBroker(context, settings);
                Action<IConfigurationBuilder, IJobExecutionContext> configureAction = Configure;
                Action<IConfiguration, IServiceCollection, IJobExecutionContext> registerServicesAction = RegisterServices;
                var result = method.Invoke(instance, new object[] { _broker, configureAction, registerServicesAction }) as Task;
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
            var status = jobException == null ? StatusMembers.Success : StatusMembers.Fail;

            var metadata = context.Result as JobExecutionMetadata;

            var log = new JobExecutionResult
            {
                InstanceId = context.FireInstanceId,
                Data = data,
                StartDate = context.FireTimeUtc.DateTime,
                JobName = context.JobDetail.Key.Name,
                JobGroup = context.JobDetail.Key.Group,
                JobId = "UnitTest_FixedJobId",
                TriggerName = context.Trigger.Key.Name,
                TriggerGroup = context.Trigger.Key.Group,
                TriggerId = "UnitTest_FixedTriggerId",
                Duration = Convert.ToInt32(duration),
                EndDate = endDate,
                Exception = jobException,
                EffectedRows = metadata?.EffectedRows,
                Log = metadata?.GetLog(),
                Id = -1,
                IsStopped = false,
                Retry = false,
                Status = status
            };

            return log;
        }
    }
}