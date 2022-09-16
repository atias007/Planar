using System;

namespace Planar
{
    internal sealed class Consts
    {
        public const string ConstPrefix = "__";
        public const string QuartzPrefix = "QRTZ_";
        public const string RetryCounter = "__Job_Retry_Counter";
        public const string RetrySpan = "__Job_Retry_Span";
        public const int MaxRetries = 3;

        public const string RetryTriggerGroup = "__RetryTrigger";
        public const string RetryTriggerNamePrefix = "__RetryCount";
        public const string PlanarSystemGroup = "__System";
        public static readonly string[] PreserveGroupNames = new string[] { RetryTriggerGroup, PlanarSystemGroup };

        public const string JobTypeProperties = "__Job_Properties";

        public const string JobId = "__Job_Id";
        public const string TriggerId = "__Trigger_Id";
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
        public const string PersistRunningJobsSpanVariableKey = "PLANAR_PERSISTSPAN";
        public const string ClearTraceTableOverDaysVariableKey = "PLANAR_CLEARTRACEDAYS";
        public const string SwaggerUIVariableKey = "PLANAR_SWAGGERUI";
        public const string DeveloperExceptionPageVariableKey = "PLANAR_DEVEXPAGE";
        public const string LogLevelVariableKey = "PLANAR_LOGLEVEL";
        public const string AuthenticationModeVariableKey = "PLANAR_AUTHMODE";

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
    }
}