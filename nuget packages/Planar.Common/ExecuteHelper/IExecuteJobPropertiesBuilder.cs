using System;

namespace Planar.Common
{
    public interface IExecuteJobPropertiesBuilder
    {
        IExecuteJobProperties Build();

        IExecuteJobPropertiesBuilder CancelJobAfter(TimeSpan timeSpan);

        IExecuteJobPropertiesBuilder CancelJobAfterMilliseconds(uint milliseconds);

        IExecuteJobPropertiesBuilder CancelJobAfterMinutes(uint minutes);

        IExecuteJobPropertiesBuilder CancelJobAfterSeconds(uint seconds);

        IExecuteJobPropertiesBuilder SetEnvironment(string environment);

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
    }
}