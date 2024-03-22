using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Planar.Common.Exceptions;
using Polly;
using System;
using System.Data;
using System.Linq;
using System.Text;
using EC = Planar.Common.EnvironmentVariableConsts;

namespace Planar.Common
{
    public enum AuthMode
    {
        AllAnonymous = 0,
        ViewAnonymous = 1,
        Authenticate = 2
    }

    public static class AppSettings
    {
        public static GeneralSettings General { get; private set; } = new();

        public static SmtpSettings Smtp { get; private set; } = new();

        public static AuthenticationSettings Authentication { get; private set; } = new();

        public static ClusterSettings Cluster { get; private set; } = new();

        public static RetentionSettings Retention { get; private set; } = new();

        public static DatabaseSettings Database { get; private set; } = new();

        public static MonitorSettings Monitor { get; private set; } = new();

        public static HooksSettings Hooks { get; private set; } = new();

        public static ProtectionSettings Protection { get; private set; } = new();

        public static void Initialize(IConfiguration configuration)
        {
            Console.WriteLine("[x] Initialize AppSettings");

            InitializeEnvironment(configuration);
            InitializeConnectionString(configuration);
            InitializeMaxConcurrency(configuration);
            InitializePersistanceSpan(configuration);
            InitializePorts(configuration);
            InitializeLogLevel(configuration);
            InitializeAuthentication(configuration);
            InitializeSmtp(configuration);
            InitializeMonitor(configuration);
            InitializeProtection(configuration);
            InitializeHooks(configuration);

            // Database
            Database.Provider = GetSettings(configuration, EC.DatabaseProviderVariableKey, "database", "provider", "Npgsql");
            Database.RunMigration = GetSettings(configuration, EC.RunDatabaseMigrationVariableKey, "database", "run migration", true);

            // General
            General.InstanceId = GetSettings(configuration, EC.InstanceIdVariableKey, "general", "instance id", "AUTO");
            General.ServiceName = GetSettings(configuration, EC.ServiceNameVariableKey, "general", "service name", "PlanarService");
            General.JobAutoStopSpan = GetSettings(configuration, EC.JobAutoStopSpanVariableKey, "general", "job auto stop span", TimeSpan.FromHours(2));
            General.SwaggerUI = GetSettings(configuration, EC.SwaggerUIVariableKey, "general", "swagger ui", true);
            General.OpenApiUI = GetSettings(configuration, EC.OpenApiUIVariableKey, "general", "open api ui", true);
            General.DeveloperExceptionPage = GetSettings(configuration, EC.DeveloperExceptionPageVariableKey, "general", "developer exception page", true);
            General.SchedulerStartupDelay = GetSettings(configuration, EC.SchedulerStartupDelayVariableKey, "general", "scheduler startup delay", TimeSpan.FromSeconds(30));
            General.ConcurrencyRateLimiting = GetSettings(configuration, EC.ConcurrencyRateLimitingVariableKey, "general", "concurrency rate limiting", 10);
            General.EncryptAllSettings = GetSettings(configuration, EC.EncryptAllSettingsVariableKey, "general", "encrypt all settings", false);

            // Cluster
            Cluster.Clustering = GetSettings(configuration, EC.ClusteringVariableKey, "cluster", "clustering", false);
            Cluster.CheckinInterval = GetSettings(configuration, EC.ClusteringCheckinIntervalVariableKey, "cluster", "checkin interval", TimeSpan.FromSeconds(5));
            Cluster.CheckinMisfireThreshold = GetSettings(configuration, EC.ClusteringCheckinMisfireThresholdVariableKey, "cluster", "checkin misfire threshold", TimeSpan.FromSeconds(5));
            Cluster.HealthCheckInterval = GetSettings(configuration, EC.ClusterHealthCheckIntervalVariableKey, "cluster", "health check interval", TimeSpan.FromMinutes(1));
            Cluster.Port = GetSettings<short>(configuration, EC.ClusterPortVariableKey, "cluster", "port", 12306);

            // Retention
            Retention.TraceRetentionDays = GetSettings(configuration, EC.TraceRetentionDaysVariableKey, "retention", "trace retention days", 365);
            Retention.JobLogRetentionDays = GetSettings(configuration, EC.JobLogRetentionDaysVariableKey, "retention", "job log retention days", 365);
            Retention.StatisticsRetentionDays = GetSettings(configuration, EC.MetricssRetentionDaysVariableKey, "retention", "statistics retention days", 365);

            if (General.ConcurrencyRateLimiting < 1)
            {
                throw new AppSettingsException($"'concurrency rate limiting' have minimum value of 1 minute. Current length is {General.ConcurrencyRateLimiting}");
            }
        }

        private static void InitializeEnvironment(IConfiguration configuration)
        {
            General.Environment = GetSettings(configuration, EC.EnvironmentVariableKey, "general", "environment", Consts.ProductionEnvironment);
            Global.Environment = General.Environment;
            if (General.Environment == Consts.ProductionEnvironment)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(string.Empty.PadLeft(40, '*'));
                Console.WriteLine($"ATTENTION: Planar is running in {Consts.ProductionEnvironment} mode");
                Console.WriteLine(string.Empty.PadLeft(40, '*'));
                Console.ResetColor();
            }
        }

        private static void InitializeSmtp(IConfiguration configuration)
        {
            Smtp.Host = GetSettings(configuration, EC.SmtpHost, "smtp", "host", string.Empty);
            Smtp.Port = GetSettings(configuration, EC.SmtpPort, "smtp", "port", 25);
            Smtp.FromAddress = GetSettings(configuration, EC.SmtpFromAddress, "smtp", "from address", string.Empty);
            Smtp.FromName = GetSettings(configuration, EC.SmtpFromName, "smtp", "from name", string.Empty);
            Smtp.Username = GetSettings(configuration, EC.SmtpUsername, "smtp", "username", string.Empty);
            Smtp.Password = GetSettings(configuration, EC.SmtpPassword, "smtp", "password", string.Empty);
            Smtp.UseDefaultCredentials = GetSettings(configuration, EC.UseSmtpDefaultCredentials, "smtp", "default credentials", false);
        }

        private static void InitializeProtection(IConfiguration configuration)
        {
            Protection.MaxMemoryUsage = GetSettings(configuration, EC.ProtectionMaxMemoryUsage, "protection", "max memory usage", 5000);
            Protection.RestartOnHighMemoryUsage = GetSettings(configuration, EC.ProtectionRestartOnHighMemoryUsage, "protection", "restart on high memory usage", true);
            Protection.WaitBeforeRestart = GetSettings(configuration, EC.ProtectionWaitBeforeRestart, "protection", "wait before restart", TimeSpan.FromMinutes(5));

            if (Protection.MaxMemoryUsage < 1000)
            {
                throw new AppSettingsException($"'max memory usage' value of {Protection.MaxMemoryUsage} is invalid. minimum value is 1000");
            }

            if (Protection.WaitBeforeRestart.TotalMinutes < 1)
            {
                throw new AppSettingsException("'wait before restart' is invalid. minimum value is 1 minute");
            }
        }

        private static void InitializeMonitor(IConfiguration configuration)
        {
            Monitor.MaxAlertsPerMonitor = GetSettings(configuration, EC.MonitorMaxAlerts, "monitor", "max alerts per monitor", 10);
            Monitor.MaxAlertsPeriod = GetSettings(configuration, EC.MonitorMaxAlertsPeriod, "monitor", "max alerts period", TimeSpan.FromDays(1));
            Monitor.ManualMuteMaxPeriod = GetSettings(configuration, EC.MonitorManualMuteMaxPeriod, "monitor", "manual mute max period", TimeSpan.FromDays(1));

            if (Monitor.MaxAlertsPerMonitor < 1)
            {
                throw new AppSettingsException($"'max alerts per monitor' value of {Monitor.MaxAlertsPerMonitor} is invalid. minimum value is 1");
            }

            if (Monitor.MaxAlertsPeriod.TotalHours < 1)
            {
                throw new AppSettingsException("'max alerts period' value is invalid. minimum value is 1 hour");
            }

            if (Monitor.ManualMuteMaxPeriod.TotalHours < 1)
            {
                throw new AppSettingsException("'manual mute max period' value is invalid. minimum value is 1 hour");
            }
        }

        private static void InitializeHooks(IConfiguration configuration)
        {
            Hooks.Rest.DefaultUrl = GetSettings(configuration, EC.HooksRestDefaultUrl, "hooks:rest", "default url", string.Empty);
            Hooks.Teams.DefaultUrl = GetSettings(configuration, EC.MonitorMaxAlertsPeriod, "hooks:teams", "default url", string.Empty);
            Hooks.Teams.SendToMultipleChannels = GetSettings(configuration, EC.HooksTeamsSendToMultipleChannels, "hooks:teams", "send to multiple channels", false);
            Hooks.TwilioSms.AccountSid = GetSettings(configuration, EC.HooksTwilioSmsAccountSid, "hooks:twilio sms", "account sid", string.Empty);
            Hooks.TwilioSms.AuthToken = GetSettings(configuration, EC.HooksTwilioSmsAuthToken, "hooks:twilio sms", "auth token", string.Empty);
            Hooks.TwilioSms.FromNumber = GetSettings(configuration, EC.HooksTwilioSmsFromNumber, "hooks:twilio sms", "from number", string.Empty);
            Hooks.TwilioSms.DefaultPhonePrefix = GetSettings(configuration, EC.HooksTwilioSmsDefaultPhonePrefix, "hooks:twilio sms", "default phone prefix", string.Empty);
            Hooks.Redis.Endpoints = [.. GetSettings(configuration, EC.HooksRedisEndpoints, "hooks:redis", "endpoints", string.Empty).Split(',')];
            Hooks.Redis.Password = GetSettings(configuration, EC.HooksRedisPassword, "hooks:redis", "password", string.Empty);
            Hooks.Redis.User = GetSettings(configuration, EC.HooksRedisUser, "hooks:redis", "user", string.Empty);
            Hooks.Redis.Database = GetSettings(configuration, EC.HooksRedisDatabase, "hooks:redis", "db", (ushort)0);
            Hooks.Redis.StreamName = GetSettings(configuration, EC.HooksRedisStreamName, "hooks:redis", "stream name", string.Empty);
            Hooks.Redis.PubSubChannel = GetSettings(configuration, EC.HooksRedisPubSubChannel, "hooks:redis", "pub sub channel", string.Empty);
            Hooks.Redis.Ssl = GetSettings(configuration, EC.HooksRedisSsl, "hooks:redis", "ssl", false);
            Hooks.Telegram.BotToken = GetSettings(configuration, EC.HooksTelegramBotToken, "hooks:telegram", "bot token", string.Empty);
            Hooks.Telegram.ChatId = GetSettings(configuration, EC.HooksTelegramChatId, "hooks:telegram", "chat id", string.Empty);
        }

        private static void InitializePersistanceSpan(IConfiguration configuration)
        {
            General.PersistRunningJobsSpan = GetSettings<TimeSpan>(configuration, EC.PersistRunningJobsSpanVariableKey, "general", "persist running jobs span");

            if (General.PersistRunningJobsSpan == TimeSpan.Zero)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"WARNING: 'persist running jobs span' settings is Zero (00:00:00). Set to default value {Consts.PersistRunningJobsSpanDefaultValue}");
                Console.ResetColor();
                General.PersistRunningJobsSpan = Consts.PersistRunningJobsSpanDefaultValue;
            }
        }

        private static void InitializeMaxConcurrency(IConfiguration configuration)
        {
            General.MaxConcurrency = GetSettings<int>(configuration, EC.MaxConcurrencyVariableKey, "general", "max concurrency");

            if (General.MaxConcurrency == default)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"WARNING: 'max concurrency' settings is null. Set to default value {Consts.MaxConcurrencyDefaultValue}");
                Console.ResetColor();
                General.MaxConcurrency = Consts.MaxConcurrencyDefaultValue;
            }

            if (General.MaxConcurrency < 1)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"WARNING: 'max concurrency' settings is less then 1 ({General.MaxConcurrency}). Set to default value {Consts.MaxConcurrencyDefaultValue}");
                Console.ResetColor();
                General.MaxConcurrency = Consts.MaxConcurrencyDefaultValue;
            }
        }

        private static void InitializeConnectionString(IConfiguration configuration)
        {
            Database.ConnectionString = GetSettings(configuration, EC.ConnectionStringVariableKey, "database", "connection string", string.Empty);

            if (string.IsNullOrEmpty(Database.ConnectionString))
            {
                throw new AppSettingsException($"ERROR: 'database connection' string could not be initialized\r\nMissing key 'connection string' or value is empty in AppSettings.yml file and there is no environment variable '{EC.ConnectionStringVariableKey}'");
            }

            try
            {
                var builder = new SqlConnectionStringBuilder(Database.ConnectionString);
                if (!builder.MultipleActiveResultSets) { return; }

                builder.MultipleActiveResultSets = false;
                Database.ConnectionString = builder.ConnectionString;
            }
            catch (Exception ex)
            {
                throw new AppSettingsException($"ERROR: 'database connection' is not valid\r\nerror message: {ex.Message}\r\nconnection string: {Database.ConnectionString}");
            }
        }

        public static void TestDatabasePermission()
        {
            try
            {
                using var conn = new SqlConnection(Database.ConnectionString);

                var cmd = new CommandDefinition(
                    commandText: "admin.TestPermission",
                    commandType: CommandType.StoredProcedure);

                conn.ExecuteAsync(cmd).Wait();
                Console.WriteLine($"    - Test database permission success");
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                var seperator = string.Empty.PadLeft(80, '-');
                sb.AppendLine("fail to test database permissions");
                sb.AppendLine(seperator);
                sb.AppendLine(Database.ConnectionString);
                sb.AppendLine(seperator);
                sb.AppendLine("exception message:");
                sb.AppendLine(ex.Message);
                throw new AppSettingsException(sb.ToString());
            }
        }

        public static void TestConnectionString()
        {
            var connectionString = Database.ConnectionString;
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new AppSettingsException("connection string is null or empty");
            }

            if (!connectionString.Contains("Connection Timeout", StringComparison.CurrentCultureIgnoreCase))
            {
                connectionString = $"{connectionString};Connection Timeout=3";
            }

            try
            {
                var counter = 1;
                Policy.Handle<SqlException>()
                    .WaitAndRetryAsync(12, i => TimeSpan.FromSeconds(5))
                    .ExecuteAsync(() =>
                    {
                        Console.WriteLine($"    - Attemp no {counter++} to connect to database");
                        using var conn = new SqlConnection(connectionString);
                        return conn.OpenAsync();
                    });

                Console.WriteLine($"    - Connection database success");
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                var seperator = string.Empty.PadLeft(80, '-');
                sb.AppendLine("fail to open connection to database using connection string");
                sb.AppendLine(seperator);
                sb.AppendLine(connectionString);
                sb.AppendLine(seperator);
                sb.AppendLine("exception message:");
                sb.AppendLine(ex.Message);
                throw new AppSettingsException(sb.ToString());
            }
        }

        private static void InitializePorts(IConfiguration configuration)
        {
            General.HttpPort = GetSettings<short>(configuration, EC.HttpPortVariableKey, "general", "http port", 2306);
            General.HttpsPort = GetSettings<short>(configuration, EC.HttpsPortVariableKey, "general", "https port", 2610);
            General.UseHttps = GetSettings(configuration, EC.UseHttpsVariableKey, "general", "use https", false);
            General.UseHttpsRedirect = GetSettings(configuration, EC.UseHttpsRedirectVariableKey, "general", "use https redirect", true);
        }

        private static void InitializeLogLevel(IConfiguration configuration)
        {
            var level = GetSettings(configuration, EC.LogLevelVariableKey, "general", "log level", LogLevel.Information.ToString());
            if (Enum.TryParse<LogLevel>(level, true, out var tempLevel))
            {
                General.LogLevel = tempLevel;
            }
            else
            {
                General.LogLevel = LogLevel.Information;
            }

            Global.LogLevel = General.LogLevel;
        }

        private static void InitializeAuthentication(IConfiguration configuration)
        {
            const string DefaultAuthenticationSecret = "ecawiasqrpqrgyhwnolrudpbsrwaynbqdayndnmcehjnwqyouikpodzaqxivwkconwqbhrmxfgccbxbyljguwlxhdlcvxlutbnwjlgpfhjgqbegtbxbvwnacyqnltrby";

            var mode = GetSettings(configuration, EC.AuthenticationModeVariableKey, "authentication", "mode", AuthMode.AllAnonymous.ToString());
            Authentication.Secret = GetSettings(configuration, EC.AuthenticationSecretVariableKey, "authentication", "secret", DefaultAuthenticationSecret);
            Authentication.TokenExpire = GetSettings(configuration, EC.AuthenticationTokenExpireVariableKey, "authentication", "token expire", TimeSpan.FromMinutes(20));

            mode = mode.Replace(" ", string.Empty);
            if (Enum.TryParse<AuthMode>(mode, true, out var tempMode))
            {
                Authentication.Mode = tempMode;
            }
            else
            {
                throw new AppSettingsException($"authentication mode {mode} is invalid");
            }

            if (Authentication.Mode == AuthMode.AllAnonymous) { return; }

            if (string.IsNullOrEmpty(Authentication.Secret))
            {
                throw new AppSettingsException($"authentication secret must have value when authentication mode is {Authentication.Mode}");
            }

            if (Authentication.Secret.Length < 65)
            {
                throw new AppSettingsException($"authentication secret must have minimum length of 65 charecters. Current length is {Authentication.Secret.Length}");
            }

            if (Authentication.Secret.Length > 256)
            {
                throw new AppSettingsException($"authentication secret must have maximum length of 256 charecters. Current length is {Authentication.Secret.Length}");
            }

            if (Authentication.TokenExpire.TotalMinutes < 1)
            {
                throw new AppSettingsException($"authentication token expire have minimum value of 1 minute. Current length is {Authentication.TokenExpire.TotalSeconds:N0} seconds");
            }

            Authentication.Key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Authentication.Secret));
        }

        public static T GetSettings<T>(IConfiguration configuration, string environmentKey, string section, string appSettingsKey, T defaultValue = default)
            where T : struct
        {
            // Environment Variable
            var property = configuration.GetValue<T?>(environmentKey);

            // AppSettings File
            property ??= configuration.GetValue<T?>($"{section}:{appSettingsKey}");

            // Default Value
            property ??= defaultValue;

            return property.GetValueOrDefault();
        }

        public static string GetSettings(IConfiguration configuration, string environmentKey, string section, string appSettingsKey, string defaultValue)
        {
            // Environment Variable
            var property = configuration.GetValue<string>(environmentKey);

            // AppSettings File
            property ??= configuration.GetValue<string>($"{section}:{appSettingsKey}");

            // Default Value
            property ??= defaultValue;

            return property;
        }
    }
}