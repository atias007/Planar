using System;

namespace Planar.Common
{
    public class ExecuteJobPropertiesBuilder
    {
        private readonly ExecuteJobProperties _properties = new ExecuteJobProperties();

        private ExecuteJobPropertiesBuilder(Type type)
        {
            _properties.JobType = type;
        }

        public static ExecuteJobPropertiesBuilder CreateBuilderForJob<T>() where T : class, new() => new ExecuteJobPropertiesBuilder(typeof(T));

        public ExecuteJobPropertiesBuilder SetRecoveringMode()
        {
            _properties.Recovering = true;
            return this;
        }

        public ExecuteJobPropertiesBuilder WithRefireCount(uint refireCount)
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

        public ExecuteJobPropertiesBuilder SetEnvironment(string environment)
        {
            if (string.IsNullOrWhiteSpace(environment))
            {
                throw new ExecuteJobBuilderException("environment parameter should not be empty");
            }

            _properties.Environment = environment;
            return this;
        }

        public ExecuteJobPropertiesBuilder WithJobName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ExecuteJobBuilderException("job name parameter should not be empty");
            }

            _properties.JobKeyName = name;
            return this;
        }

        public ExecuteJobPropertiesBuilder WithJobGroup(string group)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                throw new ExecuteJobBuilderException("job group parameter should not be empty");
            }

            _properties.JobKeyGroup = group;
            return this;
        }

        public ExecuteJobPropertiesBuilder WithJobKey(string group, string name)
        {
            return WithJobGroup(group).WithJobName(name);
        }

        public ExecuteJobPropertiesBuilder WithTriggerName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ExecuteJobBuilderException("trigger name parameter should not be empty");
            }

            _properties.TriggerKeyName = name;
            return this;
        }

        public ExecuteJobPropertiesBuilder WithExecutionDate(DateTimeOffset executionDate)
        {
            _properties.ExecutionDate = executionDate;
            return this;
        }

        public ExecuteJobPropertiesBuilder WithJobData(string key, object? value)
        {
            _properties.JobData.Add(key, value);
            return this;
        }

        public ExecuteJobPropertiesBuilder WithTriggerData(string key, object? value)
        {
            _properties.TriggerData.Add(key, value);
            return this;
        }

        public ExecuteJobPropertiesBuilder WithGlobalSettings(string key, object? value)
        {
            _properties.GlobalSettings.Add(key, PlanarConvert.ToString(value));
            return this;
        }

        public ExecuteJobPropertiesBuilder CancelJobAfter(TimeSpan timeSpan)
        {
            if (timeSpan == TimeSpan.Zero)
            {
                throw new ExecuteJobBuilderException("timeSpan parameter should be greater then TimeSpan.Zero");
            }

            _properties.CancelJobAfter = timeSpan;
            return this;
        }

        public ExecuteJobPropertiesBuilder CancelJobAfterSeconds(uint seconds)
        {
            if (seconds == 0)
            {
                throw new ExecuteJobBuilderException("seconds parameter should be greater then 0");
            }

            _properties.CancelJobAfter = TimeSpan.FromSeconds(seconds);
            return this;
        }

        public ExecuteJobPropertiesBuilder CancelJobAfterMinutes(uint minutes)
        {
            if (minutes == 0)
            {
                throw new ExecuteJobBuilderException("minutes parameter should be greater then 0");
            }

            _properties.CancelJobAfter = TimeSpan.FromMinutes(minutes);
            return this;
        }

        public ExecuteJobPropertiesBuilder CancelJobAfterMilliseconds(uint milliseconds)
        {
            if (milliseconds == 0)
            {
                throw new ExecuteJobBuilderException("milliseconds parameter should be greater then 0");
            }

            _properties.CancelJobAfter = TimeSpan.FromMilliseconds(milliseconds);
            return this;
        }

        public IExecuteJobProperties Build()
        {
            return _properties;
        }
    }
}