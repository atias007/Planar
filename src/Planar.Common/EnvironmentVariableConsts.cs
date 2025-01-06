namespace Planar.Common
{
    public static class EnvironmentVariableConsts
    {
        public const string EnvironmentVariableKey = "PLANAR_ENVIRONMENT";
        public const string ConnectionStringVariableKey = "PLANAR_DB_CONNECTION_STRING";
        public const string DatabaseProviderVariableKey = "PLANAR_DB_PROVIDER";
        public const string MaxConcurrencyVariableKey = "PLANAR_MAX_CONCURRENCY";
        public const string JobAutoStopSpanVariableKey = "PLANAR_AUTO_STOP_SPAN";
        public const string PersistRunningJobsSpanVariableKey = "PLANAR_PERSIST_SPAN";
        public const string TraceRetentionDaysVariableKey = "PLANAR_TRACE_RETENTION_DAYS";
        public const string JobLogRetentionDaysVariableKey = "PLANAR_JOB_LOG_RETENTION_DAYS";
        public const string MetricssRetentionDaysVariableKey = "PLANAR_METRICS_RETENTION_DAYS";
        public const string SwaggerUIVariableKey = "PLANAR_SWAGGER_UI";
        public const string OpenApiUIVariableKey = "PLANAR_OPENAPI_UI";
        public const string DeveloperExceptionPageVariableKey = "PLANAR_DEVELOPER_EXEPTION_PAGE";
        public const string LogLevelVariableKey = "PLANAR_LOG_LEVEL";
        public const string AuthenticationModeVariableKey = "PLANAR_AUTHENTICATION_MODE";
        public const string AuthenticationSecretVariableKey = "PLANAR_AUTHENTICATION_SECRET";
        public const string AuthenticationTokenExpireVariableKey = "PLANAR_AUTHENTICATION_TOKEN_EXPIRE";
        public const string SchedulerStartupDelayVariableKey = "PLANAR_SCHEDULER_DELAY";
        public const string RunDatabaseMigrationVariableKey = "PLANAR_RUN_DB_MIGRATION";
        public const string ConcurrencyRateLimitingVariableKey = "PLANAR_CONCURRENCY_RATE_LIMITING";

        public const string InstanceIdVariableKey = "PLANAR_INSTANCE_ID";
        public const string ServiceNameVariableKey = "PLANAR_SERVICE_NAME";
        public const string ClusteringVariableKey = "PLANAR_CLUSTERING";
        public const string ClusteringCheckinIntervalVariableKey = "PLANAR_CHECKIN_INTERVAL";
        public const string ClusteringCheckinMisfireThresholdVariableKey = "PLANAR_MISFIRE_THRESHOLD";
        public const string ClusterHealthCheckIntervalVariableKey = "PLANAR_CLUSTER_HEALTH_CCHECK_INTERVAL";
        public const string ClusterPortVariableKey = "PLANAR_CLUSTE_RPORT";
        public const string EncryptAllSettingsVariableKey = "PLANAR_ENCRYPT_ALL_SETTINGS";

        public const string HttpPortVariableKey = "PLANAR_HTTP_PORT";
        public const string HttpsPortVariableKey = "PLANAR_HTTPS_PORT";
        public const string JobPortVariableKey = "PLANAR_JOB_PORT";
        public const string UseHttpsRedirectVariableKey = "PLANAR_HTTPS_REDIRECT";
        public const string UseHttpsVariableKey = "PLANAR_HTTPS";

        public const string SmtpHost = "PLANAR_SMTP_HOST";
        public const string SmtpPort = "PLANAR_SMTP_PORT";
        public const string SmtpFromAddress = "PLANAR_SMTP_FROM_ADDRESS";
        public const string SmtpFromName = "PLANAR_SMTP_FROM_NAME";
        public const string SmtpUsername = "PLANAR_SMTP_USERNAME";
        public const string SmtpPassword = "PLANAR_SMTP_PASSWORD";
        public const string UseSmtpDefaultCredentials = "PLANAR_SMTP_USE_DEFAULT_CREDENTIALS";
        public const string SmtpHtmlImageMode = "PLANAR_SMTP_HTML_IMAGE_MODE";
        public const string SmtpHtmlImageInternalBaseUrl = "PLANAR_SMTP_HTML_IMAGE_INTERNAL_BASE_URL";

        public const string MonitorMaxAlerts = "PLANAR_MONITOR_MAX_ALERTS";
        public const string MonitorMaxAlertsPeriod = "PLANAR_MONITOR_MAX_ALERTS_PERIOD";
        public const string MonitorManualMuteMaxPeriod = "PLANAR_MONITOR_MANUAL_MUTE_PERIOD";

        public const string ProtectionMaxMemoryUsage = "PLANAR_PROTECTION_MAX_MEMORY_USAGE";
        public const string ProtectionRestartOnHighMemoryUsage = "PLANAR_PROTECTION_RESTART_ON_HIGH_MEMORY_USAGE";
        public const string ProtectionWaitBeforeRestart = "PLANAR_PROTECTION_WAIT_BEFORE_RESTART";
        public const string RegularRestartExpression = "PLANAR_PROTECTION_REGULAR_RESTART_EXPRESSION";

        public const string HooksRestDefaultUrl = "PLANAR_HOOKS_REST_DEFAULT_URL";
        public const string HooksTeamsDefaultUrl = "PLANAR_HOOKS_TEAMS_DEFAULT_URL";
        public const string HooksTeamsSendToMultipleChannels = "PLANAR_HOOKS_TEAMS_SEND_TO_MULTIPLE_CHANNELS";
        public const string HooksTwilioSmsAccountSid = "PLANAR_HOOKS_TWILIO_SMS_ACCOUNT_SID";
        public const string HooksTwilioSmsAuthToken = "PLANAR_HOOKS_TWILIO_SMS_AUTH_TOKEN";
        public const string HooksTwilioSmsFromNumber = "PLANAR_HOOKS_TWILIO_SMS_FROM_NUMBER";
        public const string HooksTwilioSmsDefaultPhonePrefix = "PLANAR_HOOKS_TWILIO_SMS_DEFAULT_PHONE_PREFIX";
        public const string HooksRedisEndpoints = "PLANAR_HOOKS_REDIS_ENDPOINTS";
        public const string HooksRedisPassword = "PLANAR_HOOKS_REDIS_PASSWORD";
        public const string HooksRedisUser = "PLANAR_HOOKS_REDIS_USER";
        public const string HooksRedisDatabase = "PLANAR_HOOKS_REDIS_DATABASE";
        public const string HooksRedisStreamName = "PLANAR_HOOKS_REDIS_STREAM_NAME";
        public const string HooksRedisPubSubChannel = "PLANAR_HOOKS_REDIS_PUBSUB_CHANNEL";
        public const string HooksRedisSsl = "PLANAR_HOOKS_REDIS_SSL";
        public const string HooksTelegramBotToken = "PLANAR_HOOKS_TELEGRAM_BOT_TOKEN";
        public const string HooksTelegramChatId = "PLANAR_HOOKS_TELEGRAM_CHAT_ID";
    }
}