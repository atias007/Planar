﻿using System;

namespace Planar.Hook
{
    public interface IMonitorDetailsBuilder : IMonitorBuilder<IMonitorDetailsBuilder>
    {
        IMonitorDetails Build();

        IMonitorDetailsBuilder WithAuthor(string author);

        IMonitorDetailsBuilder WithCalendar(string calendar);

        IMonitorDetailsBuilder SetDurable();

        IMonitorDetailsBuilder WithFireInstanceId(string fireInstanceId);

        IMonitorDetailsBuilder WithFireTime(DateTime fireTime);

        IMonitorDetailsBuilder WithJobDescription(string jobDescription);

        IMonitorDetailsBuilder WithJobGroup(string jobGroup);

        IMonitorDetailsBuilder WithJobId(string jobId);

        IMonitorDetailsBuilder WithJobName(string jobName);

        IMonitorDetailsBuilder WithJobRunTime(TimeSpan jobRunTime);

#if NETSTANDARD2_0

        IMonitorDetailsBuilder AddDataMap(string key, string value);

#else
        IMonitorDetailsBuilder AddDataMap(string key, string? value);
#endif

        IMonitorDetailsBuilder SetRecovering();

        IMonitorDetailsBuilder WithTriggerGroup(string triggerGroup);

        IMonitorDetailsBuilder WithTriggerId(string triggerId);

        IMonitorDetailsBuilder WithTriggerName(string triggerName);
    }
}