using System;

namespace Planar.Common
{
    public interface IExecuteJobPropertiesBuilder
    {
        IExecuteJobProperties Build();

        IExecuteJobPropertiesBuilder WithEnvironment(string environment);

        IExecuteJobPropertiesBuilder SetRecoveringMode();

        IExecuteJobPropertiesBuilder WithExecutionDate(DateTimeOffset executionDate);

        IExecuteJobPropertiesBuilder WithGlobalSettings(string key, object? value);

        IExecuteJobPropertiesBuilder WithJobData(string key, object? value);

        IExecuteJobPropertiesBuilder WithJobGroup(string group);

        IExecuteJobPropertiesBuilder WithJobKey(string group, string name);

        IExecuteJobPropertiesBuilder WithJobName(string name);

        IExecuteJobPropertiesBuilder WithRefireCount(uint refireCount);

        IExecuteJobPropertiesBuilder WithTriggerData(string key, object? value);

        IExecuteJobPropertiesBuilder WithTriggerName(string name);

        IExecuteJobPropertiesBuilder WithTriggerTimeout(TimeSpan timeout);
    }
}