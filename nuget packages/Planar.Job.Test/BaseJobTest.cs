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
            where T : BaseJob, new()
        {
            var props = new ExecuteJobProperties
            {
                JobType = typeof(T)
            };

            return ExecuteJob<T>(props);
        }

        protected IJobExecutionResult ExecuteJob<T>(IExecuteJobPropertiesBuilder builder)
            where T : BaseJob, new()
        {
            var props = builder.Build();
            return ExecuteJob<T>(props);
        }

        protected IExecuteJobPropertiesBuilder CreateJobPropertiesBuilder()
        {
            return new ExecuteJobPropertiesBuilder();
        }

        protected abstract void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context);

#if NETSTANDARD2_0

        private static MethodInfo ValidateAndGetExecutionMethod(Type type)
#else
        private static MethodInfo ValidateAndGetExecutionMethod(Type? type)
#endif
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

        private IJobExecutionResult ExecuteJob<T>(IExecuteJobProperties properties)
            where T : BaseJob, new()
        {
            var jobType = typeof(T);

            // Build mock of  IJobExecutionContext
            var context = new MockJobExecutionContext(properties);

            // Get Execution Method
            var method = ValidateAndGetExecutionMethod(jobType);

            // Create Job Instance
            var instance = Activator.CreateInstance(jobType);

            // Map instance properties (from job Merged Data Map)
            var mapper = new JobMapper();
            mapper.MapJobInstanceProperties(context, instance);

            // Load Job Setting & merge with Global Settings
            var settings = JobSettingsLoader.LoadJobSettingsForUnitTest(jobType).Merge(context.JobSettings);
            context.JobSettings = settings;

            // Serialize Job Context
            var json = JsonSerializer.Serialize(context);
#if NETSTANDARD2_0
            Exception jobException = null;
            object baseJob = null;
#else
            Exception? jobException = null;
            object? baseJob = null;
#endif

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

#if NETSTANDARD2_0

        private static JobExecutionResult GetJobExecutionResult(MockJobExecutionContext context, object instance, Exception jobException, object baseJob)
#else
        private static JobExecutionResult GetJobExecutionResult(MockJobExecutionContext context, object instance, Exception? jobException, object? baseJob)
#endif
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