using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Planar.Common;
using Planar.Job.Test.Common;
using System;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace Planar.Job.Test
{
    public abstract class BaseJobTest
    {
        protected abstract void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context);

        protected IJobExecutionResult ExecuteJob<T>()
            where T : class, new()
        {
            var props = new ExecuteJobProperties
            {
                JobType = typeof(T)
            };

            return ExecuteJob(props);
        }

        protected IJobExecutionResult ExecuteJob(IExecuteJobPropertiesBuilder builder)
        {
            var props = builder.Build();
            return ExecuteJob(props);
        }

        protected IExecuteJobPropertiesBuilder CreateJobPropertiesBuilder<TJob>()
            where TJob : BaseJob, new()
        {
            return new ExecuteJobPropertiesBuilder(typeof(TJob));
        }

        protected abstract void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context);

        private static MethodInfo ValidateBaseJob(Type? type)
        {
            if (type == null)
            {
                throw new PlanarJobTestException("Job type is null");
            }

#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
            var method = type.GetMethod("ExecuteUnitTest", BindingFlags.NonPublic | BindingFlags.Instance) ??
                throw new PlanarJobTestException($"Type '{type.Name}' has no 'ExecuteUnitTest' method");
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

            var parameters = method.GetParameters();
            if (parameters?.Length != 3)
            {
                throw new PlanarJobTestException($"Method 'Execute' at type '{type.Name}' must have only 1 parameters (current parameters count {parameters?.Length})");
            }

            if (!parameters[0].ParameterType.ToString().StartsWith("System.String"))
            {
                throw new PlanarJobTestException($"First parameter in method 'Execute' at type '{type.Name}' must be object. (current type '{parameters[0].ParameterType.Name}')");
            }

            return method;
        }

        private IJobExecutionResult ExecuteJob(IExecuteJobProperties properties)
        {
            var context = new MockJobExecutionContext(properties);
            var method = ValidateBaseJob(properties.JobType);
            if (properties.JobType == null) { return JobExecutionResult.Empty; }

            var instance = Activator.CreateInstance(properties.JobType);
            var mapper = new JobMapper();
            mapper.MapJobInstanceProperties(context, instance);
            var settings = JobSettingsLoader.LoadJobSettingsForUnitTest(properties.JobType);
            settings = settings.Merge(properties.GlobalSettings);
            context.JobSettings = settings;
            var json = JsonSerializer.Serialize(context);
            Exception? jobException = null;

            // JobMessageBroker _broker = null!;

            try
            {
                // _broker = new JobMessageBroker(context, properties, settings);
                Action<IConfigurationBuilder, IJobExecutionContext> configureAction = Configure;
                Action<IConfiguration, IServiceCollection, IJobExecutionContext> registerServicesAction = RegisterServices;
                var result = method.Invoke(instance, new object[] { json, configureAction, registerServicesAction }) as Task;
                result?.ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                jobException = ex;
            }

            var duration = context.JobRunTime.TotalMilliseconds;
            var endDate = context.FireTimeUtc.DateTime.Add(context.JobRunTime);
            var status = jobException == null ? StatusMembers.Success : StatusMembers.Fail;

            var metadata = context.Result as JobExecutionMetadata;

            var log = new JobExecutionResult
            {
                InstanceId = context.FireInstanceId,
                Data = context.MergedJobDataMap,
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
                IsCanceled = false,
                Retry = false,
                Status = status,
                Instance = instance
            };

            return log;
        }
    }
}