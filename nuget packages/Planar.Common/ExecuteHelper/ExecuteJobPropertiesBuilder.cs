using System;

namespace Planar.Common
{
    internal class ExecuteJobPropertiesBuilder : IExecuteJobPropertiesBuilder
    {
        private readonly ExecuteJobProperties _properties = new ExecuteJobProperties();

        internal ExecuteJobPropertiesBuilder(Type type)
        {
            _properties.JobType = type;
        }

        public IExecuteJobPropertiesBuilder SetRecoveringMode()
        {
            _properties.Recovering = true;
            return this;
        }

        public IExecuteJobPropertiesBuilder WithRefireCount(uint refireCount)
        {
            if (refireCount == 0)
            {
                throw new ExecuteJobPropertiesBuilderException("refireCount parameter should be greater then 0");
            }

            if (refireCount > int.MaxValue)
            {
                throw new ExecuteJobPropertiesBuilderException($"refireCount parameter should be less then then {int.MaxValue}");
            }

            _properties.RefireCount = Convert.ToInt32(refireCount);
            return this;
        }

        public IExecuteJobPropertiesBuilder SetEnvironment(string environment)
        {
            if (string.IsNullOrWhiteSpace(environment))
            {
                throw new ExecuteJobPropertiesBuilderException("environment parameter should not be empty");
            }

            _properties.Environment = environment;
            return this;
        }

        public IExecuteJobPropertiesBuilder SetDevelopmentEnvironment()
        {
            const string development = "Development";
            return SetEnvironment(development);
        }

        public IExecuteJobPropertiesBuilder SetUnitTestEnvironment()
        {
            const string development = "UnitTest";
            return SetEnvironment(development);
        }

        public IExecuteJobPropertiesBuilder WithJobName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ExecuteJobPropertiesBuilderException("job name parameter should not be empty");
            }

            _properties.JobKeyName = name;
            return this;
        }

        public IExecuteJobPropertiesBuilder WithJobGroup(string group)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                throw new ExecuteJobPropertiesBuilderException("job group parameter should not be empty");
            }

            _properties.JobKeyGroup = group;
            return this;
        }

        public IExecuteJobPropertiesBuilder WithJobKey(string group, string name)
        {
            return WithJobGroup(group).WithJobName(name);
        }

        public IExecuteJobPropertiesBuilder WithTriggerName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ExecuteJobPropertiesBuilderException("trigger name parameter should not be empty");
            }

            _properties.TriggerKeyName = name;
            return this;
        }

        public IExecuteJobPropertiesBuilder WithExecutionDate(DateTimeOffset executionDate)
        {
            _properties.ExecutionDate = executionDate;
            return this;
        }

        public IExecuteJobPropertiesBuilder WithJobData(string key, object? value)
        {
            _properties.JobData.Add(key, value);
            return this;
        }

        public IExecuteJobPropertiesBuilder WithTriggerData(string key, object? value)
        {
            _properties.TriggerData.Add(key, value);
            return this;
        }

        public IExecuteJobPropertiesBuilder WithGlobalSettings(string key, object? value)
        {
            _properties.GlobalSettings.Add(key, PlanarConvert.ToString(value));
            return this;
        }

        public IExecuteJobPropertiesBuilder CancelJobAfter(TimeSpan timeSpan)
        {
            if (timeSpan == TimeSpan.Zero)
            {
                throw new ExecuteJobPropertiesBuilderException("timeSpan parameter should be greater then TimeSpan.Zero");
            }

            _properties.CancelJobAfter = timeSpan;
            return this;
        }

        public IExecuteJobPropertiesBuilder CancelJobAfterSeconds(uint seconds)
        {
            if (seconds == 0)
            {
                throw new ExecuteJobPropertiesBuilderException("seconds parameter should be greater then 0");
            }

            _properties.CancelJobAfter = TimeSpan.FromSeconds(seconds);
            return this;
        }

        public IExecuteJobPropertiesBuilder CancelJobAfterMinutes(uint minutes)
        {
            if (minutes == 0)
            {
                throw new ExecuteJobPropertiesBuilderException("minutes parameter should be greater then 0");
            }

            _properties.CancelJobAfter = TimeSpan.FromMinutes(minutes);
            return this;
        }

        public IExecuteJobPropertiesBuilder CancelJobAfterMilliseconds(uint milliseconds)
        {
            if (milliseconds == 0)
            {
                throw new ExecuteJobPropertiesBuilderException("milliseconds parameter should be greater then 0");
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