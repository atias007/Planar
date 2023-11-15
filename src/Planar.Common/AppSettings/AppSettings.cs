using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Planar.Common.Exceptions;
using Polly;
using System;
using System.Data;
using System.Text;

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

            // Database
            Database.Provider = GetSettings(configuration, Consts.DatabaseProviderVariableKey, "database", "provider", "Npgsql");
            Database.RunMigration = GetSettings(configuration, Consts.RunDatabaseMigrationVariableKey, "database", "run migration", true);

            // General
            General.InstanceId = GetSettings(configuration, Consts.InstanceIdVariableKey, "general", "instance id", "AUTO");
            General.ServiceName = GetSettings(configuration, Consts.ServiceNameVariableKey, "general", "service name", "PlanarService");
            General.JobAutoStopSpan = GetSettings(configuration, Consts.JobAutoStopSpanVariableKey, "general", "job auto stop span", TimeSpan.FromHours(2));
            General.SwaggerUI = GetSettings(configuration, Consts.SwaggerUIVariableKey, "general", "swagger ui", true);
            General.OpenApiUI = GetSettings(configuration, Consts.OpenApiUIVariableKey, "general", "open api ui", true);
            General.DeveloperExceptionPage = GetSettings(configuration, Consts.DeveloperExceptionPageVariableKey, "general", "developer exception page", true);
            General.SchedulerStartupDelay = GetSettings(configuration, Consts.SchedulerStartupDelayVariableKey, "general", "scheduler startup delay", TimeSpan.FromSeconds(30));
            General.ConcurrencyRateLimiting = GetSettings(configuration, Consts.ConcurrencyRateLimitingVariableKey, "general", "concurrency rate limiting", 10);

            // Cluster
            Cluster.Clustering = GetSettings(configuration, Consts.ClusteringVariableKey, "cluster", "clustering", false);
            Cluster.CheckinInterval = GetSettings(configuration, Consts.ClusteringCheckinIntervalVariableKey, "cluster", "checkin interval", TimeSpan.FromSeconds(5));
            Cluster.CheckinMisfireThreshold = GetSettings(configuration, Consts.ClusteringCheckinMisfireThresholdVariableKey, "cluster", "checkin misfire threshold", TimeSpan.FromSeconds(5));
            Cluster.HealthCheckInterval = GetSettings(configuration, Consts.ClusterHealthCheckIntervalVariableKey, "cluster", "health check interval", TimeSpan.FromMinutes(1));
            Cluster.Port = GetSettings<short>(configuration, Consts.ClusterPortVariableKey, "cluster", "port", 12306);

            // Retention
            Retention.TraceRetentionDays = GetSettings(configuration, Consts.TraceRetentionDaysVariableKey, "retention", "trace retention days", 365);
            Retention.JobLogRetentionDays = GetSettings(configuration, Consts.JobLogRetentionDaysVariableKey, "retention", "job log retention days", 365);
            Retention.StatisticsRetentionDays = GetSettings(configuration, Consts.StatisticsRetentionDaysVariableKey, "retention", "statistics retention days", 365);

            if (General.ConcurrencyRateLimiting < 1)
            {
                throw new AppSettingsException($"Concurrency rate limiting have minimum value of 1 minute. Current length is {General.ConcurrencyRateLimiting}");
            }
        }

        private static void InitializeEnvironment(IConfiguration configuration)
        {
            General.Environment = GetSettings(configuration, Consts.EnvironmentVariableKey, "general", "environment", Consts.ProductionEnvironment);
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
            Smtp.Host = GetSettings(configuration, Consts.SmtpHost, "smtp", "host", string.Empty);
            Smtp.Port = GetSettings(configuration, Consts.SmtpPort, "smtp", "port", 25);
            Smtp.FromAddress = GetSettings(configuration, Consts.SmtpFromAddress, "smtp", "from address", string.Empty);
            Smtp.FromName = GetSettings(configuration, Consts.SmtpFromName, "smtp", "from name", string.Empty);
            Smtp.Username = GetSettings(configuration, Consts.SmtpUsername, "smtp", "username", string.Empty);
            Smtp.Password = GetSettings(configuration, Consts.SmtpPassword, "smtp", "password", string.Empty);
        }

        private static void InitializePersistanceSpan(IConfiguration configuration)
        {
            General.PersistRunningJobsSpan = GetSettings<TimeSpan>(configuration, Consts.PersistRunningJobsSpanVariableKey, "general", "persist running jobs span");

            if (General.PersistRunningJobsSpan == TimeSpan.Zero)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"WARNING: PersistRunningJobsSpan settings is Zero (00:00:00). Set to default value {Consts.PersistRunningJobsSpanDefaultValue}");
                Console.ResetColor();
                General.PersistRunningJobsSpan = Consts.PersistRunningJobsSpanDefaultValue;
            }
        }

        private static void InitializeMaxConcurrency(IConfiguration configuration)
        {
            General.MaxConcurrency = GetSettings<int>(configuration, Consts.MaxConcurrencyVariableKey, "general", "max concurrency");

            if (General.MaxConcurrency == default)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"WARNING: MaxConcurrency settings is null. Set to default value {Consts.MaxConcurrencyDefaultValue}");
                Console.ResetColor();
                General.MaxConcurrency = Consts.MaxConcurrencyDefaultValue;
            }

            if (General.MaxConcurrency < 1)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"WARNING: MaxConcurrency settings is less then 1 ({General.MaxConcurrency}). Set to default value {Consts.MaxConcurrencyDefaultValue}");
                Console.ResetColor();
                General.MaxConcurrency = Consts.MaxConcurrencyDefaultValue;
            }
        }

        private static void InitializeConnectionString(IConfiguration configuration)
        {
            Database.ConnectionString = GetSettings(configuration, Consts.ConnectionStringVariableKey, "database", "connection string", string.Empty);

            if (string.IsNullOrEmpty(Database.ConnectionString))
            {
                throw new AppSettingsException($"ERROR: database connection string could not be initialized\r\nMissing key 'connection string' or value is empty in AppSettings.yml file and there is no environment variable '{Consts.ConnectionStringVariableKey}'");
            }

            try
            {
                var builder = new SqlConnectionStringBuilder(Database.ConnectionString);
                if (builder.MultipleActiveResultSets) { return; }

                builder.MultipleActiveResultSets = true;
                Database.ConnectionString = builder.ConnectionString;
            }
            catch (Exception ex)
            {
                throw new AppSettingsException($"ERROR: database connection is not valid\r\nerror message: {ex.Message}\r\nconnection string: {Database.ConnectionString}");
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

            if (!connectionString.ToLower().Contains("Connection Timeout"))
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
            General.HttpPort = GetSettings<short>(configuration, Consts.HttpPortVariableKey, "general", "http port", 2306);
            General.HttpsPort = GetSettings<short>(configuration, Consts.HttpsPortVariableKey, "general", "https port", 2610);
            General.UseHttps = GetSettings(configuration, Consts.UseHttpsVariableKey, "general", "use https", false);
            General.UseHttpsRedirect = GetSettings(configuration, Consts.UseHttpsRedirectVariableKey, "general", "use https redirect", true);
        }

        private static void InitializeLogLevel(IConfiguration configuration)
        {
            var level = GetSettings(configuration, Consts.LogLevelVariableKey, "general", "log level", LogLevel.Information.ToString());
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

            var mode = GetSettings(configuration, Consts.AuthenticationModeVariableKey, "authentication", "mode", AuthMode.AllAnonymous.ToString());
            Authentication.Secret = GetSettings(configuration, Consts.AuthenticationSecretVariableKey, "authentication", "secret", DefaultAuthenticationSecret);
            Authentication.TokenExpire = GetSettings(configuration, Consts.AuthenticationTokenExpireVariableKey, "authentication", "token expire", TimeSpan.FromMinutes(20));

            mode = mode.Replace(" ", string.Empty);
            if (Enum.TryParse<AuthMode>(mode, true, out var tempMode))
            {
                Authentication.Mode = tempMode;
            }
            else
            {
                throw new AppSettingsException($"Authentication mode {mode} is invalid");
            }

            if (Authentication.Mode == AuthMode.AllAnonymous) { return; }

            if (string.IsNullOrEmpty(Authentication.Secret))
            {
                throw new AppSettingsException($"Authentication secret must have value when authentication mode is {Authentication.Mode}");
            }

            if (Authentication.Secret.Length < 65)
            {
                throw new AppSettingsException($"Authentication secret must have minimum length of 65 charecters. Current length is {Authentication.Secret.Length}");
            }

            if (Authentication.Secret.Length > 256)
            {
                throw new AppSettingsException($"Authentication secret must have maximum length of 256 charecters. Current length is {Authentication.Secret.Length}");
            }

            if (Authentication.TokenExpire.TotalMinutes < 1)
            {
                throw new AppSettingsException($"Authentication token expire have minimum value of 1 minute. Current length is {Authentication.TokenExpire.TotalSeconds:N0} seconds");
            }

            Authentication.Key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Authentication.Secret));
        }

        private static T GetSettings<T>(IConfiguration configuration, string environmentKey, string section, string appSettingsKey, T defaultValue = default)
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

        private static string GetSettings(IConfiguration configuration, string environmentKey, string section, string appSettingsKey, string defaultValue)
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