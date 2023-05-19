using System;

namespace Planar.Job.Test
{
    public class ExecuteJobBuilder
    {
        private ExecuteJobBuilder(Type type)
        {
            _properties.JobType = type;
        }

        private readonly ExecuteJobProperties _properties = new ExecuteJobProperties();

        public static ExecuteJobBuilder CreateBuilderForJob<T>() where T : class, new() => new ExecuteJobBuilder(typeof(T));

        public ExecuteJobBuilder WithExecutionDate(DateTimeOffset executionDate)
        {
            _properties.ExecutionDate = executionDate;
            return this;
        }

        public ExecuteJobBuilder WithJobData(string key, object? value)
        {
            _properties.JobData.Add(key, value);
            return this;
        }

        public ExecuteJobBuilder WithTriggerData(string key, object? value)
        {
            _properties.TriggerData.Add(key, value);
            return this;
        }

        public ExecuteJobBuilder WithGlobalSettings(string key, object? value)
        {
            _properties.GlobalSettings.Add(key, PlanarConvert.ToString(value));
            return this;
        }

        public ExecuteJobBuilder CancelJobAfter(TimeSpan timeSpan)
        {
            // TODO: check greater then TimeSpan.Zero
            _properties.CancelJobAfter = timeSpan;
            return this;
        }

        public ExecuteJobBuilder CancelJobAfterSeconds(uint seconds)
        {
            // TODO: check greater then 1
            _properties.CancelJobAfter = TimeSpan.FromSeconds(seconds);
            return this;
        }

        public ExecuteJobBuilder CancelJobAfterMinutes(uint seconds)
        {
            // TODO: check greater then 1
            _properties.CancelJobAfter = TimeSpan.FromMinutes(seconds);
            return this;
        }

        public ExecuteJobBuilder CancelJobAfterMilliseconds(uint seconds)
        {
            // TODO: check greater then 1
            _properties.CancelJobAfter = TimeSpan.FromMilliseconds(seconds);
            return this;
        }

        internal ExecuteJobProperties Build()
        {
            return _properties;
        }
    }
}