using System;

namespace Planar.Job.Test
{
    public class ExecuteJobBuilder
    {
        private ExecuteJobBuilder()
        {
        }

        private readonly ExecuteJobProperties _properties = new ExecuteJobProperties();

        public static ExecuteJobBuilder CreateBuilder() => new ExecuteJobBuilder();

        public ExecuteJobBuilder ForJob<T>()
            where T : class, new()
        {
            _properties.JobType = typeof(T);
            return this;
        }

        public ExecuteJobBuilder ForJob(Type type)
        {
            _properties.JobType = type;
            return this;
        }

        public ExecuteJobBuilder WithExecutionDate(DateTimeOffset executionDate)
        {
            _properties.ExecutionDate = executionDate;
            return this;
        }

        public ExecuteJobBuilder WithJobData(string key, object value)
        {
            _properties.JobData.Add(key, value);
            return this;
        }

        public ExecuteJobBuilder WithTriggerData(string key, object value)
        {
            _properties.TriggerData.Add(key, value);
            return this;
        }

        public ExecuteJobBuilder WithGlobalSettings(string key, object value)
        {
            _properties.GlobalSettings.Add(key, Convert.ToString(value));
            return this;
        }

        internal ExecuteJobProperties Build()
        {
            return _properties;
        }
    }
}