using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Planar.Common;
using Planar.Job.Test.Common;
using System;
using System.Reflection;
using System.Text.Json;

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

        private static MethodInfo ValidateAndGetExecutionMethod(Type? type)
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
            if (properties.JobType == null) { return JobExecutionResult.Empty; }

            // Build mock of  IJobExecutionContext
            var context = new MockJobExecutionContext(properties);

            // Get Execution Method
            var method = ValidateAndGetExecutionMethod(properties.JobType);

            // Create Job Instance
            var instance = Activator.CreateInstance(properties.JobType);

            // Map instance properties (from job Merged Data Map)
            var mapper = new JobMapper();
            mapper.MapJobInstanceProperties(context, instance);

            // Load Job Setting & merge with Global Settings
            var settings = JobSettingsLoader.LoadJobSettingsForUnitTest(properties.JobType);
            settings = settings.Merge(properties.GlobalSettings);
            context.JobSettings = settings;

            // Serialize Job Context
            var json = JsonSerializer.Serialize(context);
            Exception? jobException = null;
            object? baseJob = null;

            // Execute Job
            try
            {
                Action<IConfigurationBuilder, IJobExecutionContext> configureAction = Configure;
                Action<IConfiguration, IServiceCollection, IJobExecutionContext> registerServicesAction = RegisterServices;
                baseJob = method.Invoke(instance, new object[] { json, configureAction, registerServicesAction });
            }
            catch (Exception ex)
            {
                jobException = ex;
            }

            // Get result object
            var log = GetJobExecutionResult(context, instance, jobException, baseJob);
            return log;
        }

        private static JobExecutionResult GetJobExecutionResult(MockJobExecutionContext context, object instance, Exception? jobException, object? baseJob)
        {
            var duration = context.JobRunTime.TotalMilliseconds;
            var endDate = context.FireTimeUtc.DateTime.Add(context.JobRunTime);
            var status = jobException == null ? StatusMembers.Success : StatusMembers.Fail;

            var jsonResult = JsonSerializer.Serialize(baseJob);
            var unitTestResult = JsonSerializer.Deserialize<UnitTestResult>(jsonResult);

            var log = new JobExecutionResult
            {
                InstanceId = context.FireInstanceId,
                Data = context.MergedJobDataMap,
                StartDate = context.FireTimeUtc.DateTime,
                JobName = context.JobDetails.Key.Name,
                JobGroup = context.JobDetails.Key.Group,
                JobId = "UnitTest_FixedJobId",
                TriggerName = context.Trigger.Key.Name,
                TriggerGroup = context.Trigger.Key.Group,
                TriggerId = "UnitTest_FixedTriggerId",
                Duration = Convert.ToInt32(duration),
                EndDate = endDate,
                Exception = jobException,
                EffectedRows = unitTestResult?.EffectedRows,
                Log = unitTestResult?.Log,
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