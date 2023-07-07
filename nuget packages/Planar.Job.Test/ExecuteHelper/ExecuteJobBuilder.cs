using Planar.Job.Test.ExecuteHelper;
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

        public ExecuteJobBuilder SetRecoveringMode()
        {
            _properties.Recovering = true;
            return this;
        }

        public ExecuteJobBuilder WithRefireCount(uint refireCount)
        {
            if (refireCount == 0)
            {
                throw new ExecuteJobBuilderException("refireCount parameter should be greater then 0");
            }

            if (refireCount > int.MaxValue)
            {
                throw new ExecuteJobBuilderException($"refireCount parameter should be less then then {int.MaxValue}");
            }

            _properties.RefireCount = Convert.ToInt32(refireCount);
            return this;
        }

        public ExecuteJobBuilder SetEnvironment(string? environment)
        {
            if (environment == null)
            {
                throw new ExecuteJobBuilderException("environment parameter should not set to null");
            }

            _properties.Environment = environment;
            return this;
        }

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
            if (timeSpan == TimeSpan.Zero)
            {
                throw new ExecuteJobBuilderException("timeSpan parameter should be greater then TimeSpan.Zero");
            }

            _properties.CancelJobAfter = timeSpan;
            return this;
        }

        public ExecuteJobBuilder CancelJobAfterSeconds(uint seconds)
        {
            if (seconds == 0)
            {
                throw new ExecuteJobBuilderException("seconds parameter should be greater then 0");
            }

            _properties.CancelJobAfter = TimeSpan.FromSeconds(seconds);
            return this;
        }

        public ExecuteJobBuilder CancelJobAfterMinutes(uint minutes)
        {
            if (minutes == 0)
            {
                throw new ExecuteJobBuilderException("minutes parameter should be greater then 0");
            }

            _properties.CancelJobAfter = TimeSpan.FromMinutes(minutes);
            return this;
        }

        public ExecuteJobBuilder CancelJobAfterMilliseconds(uint milliseconds)
        {
            if (milliseconds == 0)
            {
                throw new ExecuteJobBuilderException("milliseconds parameter should be greater then 0");
            }

            _properties.CancelJobAfter = TimeSpan.FromMilliseconds(milliseconds);
            return this;
        }

        internal ExecuteJobProperties Build()
        {
            return _properties;
        }
    }
}