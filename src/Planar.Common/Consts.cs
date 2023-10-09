using System;

namespace Planar
{
    internal static class Consts
    {
        public static readonly string[] PreserveGroupNames = new string[] { RetryTriggerGroup, PlanarSystemGroup };

        public static readonly string[] AllDataKeys = new[]
        {
            RetryCounter,
            RetrySpan,
            MaxRetries,
            RetryTriggerGroup,
            QueueInvokeTriggerGroup,
            RetryTriggerNamePrefix,
            PlanarSystemGroup,
            JobId,
            TriggerId,
            TriggerTimeout,
            NowOverrideValue,
            Author,
            LogRetentionDays
        };

        public const int MaximumJobDataItems = 1000;
        public const string Undefined = "undefined";
        public const string Unauthorized = "unauthorized";

        public const string QuartzPrefix = "QRTZ_";
        public const string ConstPrefix = "__";
        public const string RetryCounter = "__Job_Retry_Counter";
        public const string RetrySpan = "__Job_Retry_Span";
        public const string MaxRetries = "__Job_Retry_Max";
        public const int DefaultMaxRetries = 3;

        public const string QueueInvokeTriggerGroup = "__QueueInvoke";
        public const string RetryTriggerGroup = "__RetryTrigger";
        public const string RetryTriggerNamePrefix = "Retry.Count";
        public const string PlanarSystemGroup = "__System";

        public const string JobId = "__Job_Id";
        public const string Author = "__Author";
        public const string LogRetentionDays = "__LogRetentionDays";
        public const string TriggerId = "__Trigger_Id";
        public const string TriggerTimeout = "__Trigger_Timeout";
        public const string NowOverrideValue = "__Now_Override_Value";

        public const string ManualTriggerId = "Manual";
        public const string LogLevelSettingsKey1 = "Log Level";
        public const string LogLevelSettingsKey2 = "LogLevel";

        public const int MaxConcurrencyDefaultValue = 10;
        public static readonly TimeSpan PersistRunningJobsSpanDefaultValue = TimeSpan.FromMinutes(5);
        public const string ProductionEnvironment = "Production";
        public const string RecoveringJobsGroup = "RECOVERING_JOBS";

        /// ---------------------------- Environments Variables ----------------------------
        public const string EnvironmentVariableKey = "PLANAR_ENVIRONMENT";

        public const string ConnectionStringVariableKey = "PLANAR_DBCONNSTRING";
        public const string DatabaseProviderVariableKey = "PLANAR_DBPROVIDER";
        public const string MaxConcurrencyVariableKey = "PLANAR_MAXCONCURRENCY";
        public const string JobAutoStopSpanVariableKey = "PLANAR_AUTOSTOPSPAN";
        public const string PersistRunningJobsSpanVariableKey = "PLANAR_PERSISTSPAN";
        public const string TraceRetentionDaysVariableKey = "PLANAR_TRACERETENTIONDAYS";
        public const string JobLogRetentionDaysVariableKey = "PLANAR_JOBLOGRETENTIONDAYS";
        public const string StatisticsRetentionDaysVariableKey = "PLANAR_STATSRETENTIONDAYS";
        public const string SwaggerUIVariableKey = "PLANAR_SWAGGERUI";
        public const string OpenApiUIVariableKey = "PLANAR_OPENAPIUI";
        public const string DeveloperExceptionPageVariableKey = "PLANAR_DEVEXPAGE";
        public const string LogLevelVariableKey = "PLANAR_LOGLEVEL";
        public const string AuthenticationModeVariableKey = "PLANAR_AUTHMODE";
        public const string AuthenticationSecretVariableKey = "PLANAR_AUTHSECRET";
        public const string AuthenticationTokenExpireVariableKey = "PLANAR_AUTHTOKENEXPIRE";
        public const string SchedulerStartupDelayVariableKey = "PLANAR_SCHEDULER_DELAY";
        public const string RunDatabaseMigrationVariableKey = "PLANAR_RUN_DBMIGRATION";
        public const string ConcurrencyRateLimitingVariableKey = "PLANAR_CONCURRENCY_RATELIMITING";

        public const string InstanceIdVariableKey = "PLANAR_INSTANCEID";
        public const string ServiceNameVariableKey = "PLANAR_SERVICENAME";
        public const string ClusteringVariableKey = "PLANAR_CLUSTERING";
        public const string ClusteringCheckinIntervalVariableKey = "PLANAR_CHECKININTERVAL";
        public const string ClusteringCheckinMisfireThresholdVariableKey = "PLANAR_MISFIRETHRESHOLD";
        public const string ClusterHealthCheckIntervalVariableKey = "PLANAR_CLUSTERHCINTERVAL";
        public const string ClusterPortVariableKey = "PLANAR_CLUSTERPORT";

        public const string HttpPortVariableKey = "PLANAR_HTTPPORT";
        public const string HttpsPortVariableKey = "PLANAR_HTTPSPORT";
        public const string UseHttpsRedirectVariableKey = "PLANAR_HTTPSREDIRECT";
        public const string UseHttpsVariableKey = "PLANAR_HTTPS";

        public const string SmtpHost = "PLANAR_SMTPHOST";
        public const string SmtpPort = "PLANAR_SMTPPORT";
        public const string SmtpFromAddress = "PLANAR_SMTPFROMADDRESS";
        public const string SmtpFromName = "PLANAR_SMTPFROMNAME";
        public const string SmtpUsername = "PLANAR_SMTPUSERNAME";
        public const string SmtpPassword = "PLANAR_SMTPPASSWORD";

        public static bool IsDataKeyValid(string key)
        {
            return !Array.Exists(AllDataKeys, k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
        }
    }
}