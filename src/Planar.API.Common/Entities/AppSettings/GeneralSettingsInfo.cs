using System;
using System.Collections.Generic;

namespace Planar.API.Common.Entities;

public class GeneralSettingsInfo
{
    public int MaxConcurrency { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public IEnumerable<string> InstanceIds { get; set; } = [];
    public TimeSpan JobAutoStopSpan { get; set; }
    public TimeSpan PersistRunningJobsSpan { get; set; }
    public int HttpPort { get; set; }
    public int HttpsPort { get; set; }
    public int JobPort { get; set; }
    public bool UseHttpsRedirect { get; set; }
    public bool UseHttps { get; set; }
    public string Environment { get; set; } = string.Empty;
    public bool SwaggerUI { get; set; }
    public bool OpenApiUI { get; set; }
    public bool DeveloperExceptionPage { get; set; }
    public TimeSpan SchedulerStartupDelay { get; set; }
    public int ConcurrencyRateLimiting { get; set; }
    public string LogLevel { get; set; } = string.Empty;
    public bool EncryptAllSettings { get; set; }
    public TimeSpan UpTime { get; set; }
}