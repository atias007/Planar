using System;

namespace Planar.Client.Entities
{
    public class AppSettingsInfo
    {
        public AuthenticationSettingsInfo Authentication { get; set; } = null!;
        public ClusterSettingsInfo Cluster { get; set; } = null!;
        public DatabaseSettingsInfo Database { get; set; } = null!;
        public GeneralSettingsInfo General { get; set; } = null!;
        public RetentionSettingsInfo Retention { get; set; } = null!;
        public SmtpSettingsInfo Smtp { get; set; } = null!;
        public MonitorSettingsInfo Monitor { get; set; } = null!;
    }

    public class AuthenticationSettingsInfo
    {
        public string? Mode { get; set; }
        public TimeSpan TokenExpire { get; set; }
    }

    public class ClusterSettingsInfo
    {
        public TimeSpan CheckinInterval { get; set; }
        public TimeSpan CheckinMisfireThreshold { get; set; }
        public TimeSpan HealthCheckInterval { get; set; }
        public short Port { get; set; }
        public bool Clustering { get; set; }
    }

    public class DatabaseSettingsInfo
    {
        public string? ConnectionString { get; set; }
        public string? Provider { get; set; }
        public bool RunMigration { get; set; }
    }

    public class GeneralSettingsInfo
    {
        public int MaxConcurrency { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string InstanceId { get; set; } = string.Empty;
        public TimeSpan JobAutoStopSpan { get; set; }
        public TimeSpan PersistRunningJobsSpan { get; set; }
        public short HttpPort { get; set; }
        public short HttpsPort { get; set; }
        public bool UseHttpsRedirect { get; set; }
        public bool UseHttps { get; set; }
        public string Environment { get; set; } = string.Empty;
        public bool SwaggerUI { get; set; }
        public bool OpenApiUI { get; set; }
        public bool DeveloperExceptionPage { get; set; }
        public TimeSpan SchedulerStartupDelay { get; set; }
        public int ConcurrencyRateLimiting { get; set; }
        public string LogLevel { get; set; } = string.Empty;
    }

    public class MonitorSettingsInfo
    {
        public int MaxAlertsPerMonitor { get; set; }
        public TimeSpan MaxAlertsPeriod { get; set; }
        public TimeSpan ManualMuteMaxPeriod { get; set; }
    }

    public class RetentionSettingsInfo
    {
        public int TraceRetentionDays { get; set; }
        public int JobLogRetentionDays { get; set; }
        public int StatisticsRetentionDays { get; set; }
    }

    public class SmtpSettingsInfo
    {
        public string? FromAddress { get; set; }
        public string? FromName { get; set; }
        public string Host { get; set; } = null!;
        public int Port { get; set; }
        public bool UseSsl { get; set; }
        public bool UseDefaultCredentials { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}