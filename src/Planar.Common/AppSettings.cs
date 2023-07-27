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
        public static int MaxConcurrency { get; set; }

        public static string ServiceName { get; set; } = string.Empty;

        public static string InstanceId { get; set; } = string.Empty;

        public static bool Clustering { get; set; }

        public static TimeSpan JobAutoStopSpan { get; set; }

        public static TimeSpan ClusteringCheckinInterval { get; set; }

        public static TimeSpan ClusteringCheckinMisfireThreshold { get; set; }

        public static TimeSpan ClusterHealthCheckInterval { get; set; }

        public static short ClusterPort { get; set; }

        public static string? DatabaseConnectionString { get; set; }

        public static string DatabaseProvider { get; set; } = string.Empty;

        public static TimeSpan PersistRunningJobsSpan { get; set; }

        public static int ClearTraceTableOverDays { get; set; }

        public static int ClearJobLogTableOverDays { get; set; }

        public static int ClearStatisticsTablesOverDays { get; set; }

        public static short HttpPort { get; set; }

        public static short HttpsPort { get; set; }

        public static bool UseHttpsRedirect { get; set; }

        public static bool UseHttps { get; set; }

        public static string Environment { get; set; } = string.Empty;

        public static bool SwaggerUI { get; set; }

        public static bool OpenApiUI { get; set; }

        public static bool DeveloperExceptionPage { get; set; }

        public static TimeSpan SchedulerStartupDelay { get; set; }

        public static AuthMode AuthenticationMode { get; set; }

        public static string? AuthenticationSecret { get; set; }

        public static TimeSpan AuthenticationTokenExpire { get; set; }

        public static SymmetricSecurityKey AuthenticationKey { get; set; } = null!;

        public static LogLevel LogLevel { get; set; }

        public static bool RunDatabaseMigration { get; set; }

        public static bool HasAuthontication => AuthenticationMode != AuthMode.AllAnonymous;

        public static bool NoAuthontication => AuthenticationMode == AuthMode.AllAnonymous;

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

            InstanceId = GetSettings(configuration, Consts.InstanceIdVariableKey, nameof(InstanceId), "AUTO");
            ServiceName = GetSettings(configuration, Consts.ServiceNameVariableKey, nameof(ServiceName), "PlanarService");
            Clustering = GetSettings(configuration, Consts.ClusteringVariableKey, nameof(Clustering), false);
            JobAutoStopSpan = GetSettings(configuration, Consts.JobAutoStopSpanVariableKey, nameof(JobAutoStopSpan), TimeSpan.FromHours(2));
            ClusteringCheckinInterval = GetSettings(configuration, Consts.ClusteringCheckinIntervalVariableKey, nameof(ClusteringCheckinInterval), TimeSpan.FromSeconds(5));
            ClusteringCheckinMisfireThreshold = GetSettings(configuration, Consts.ClusteringCheckinMisfireThresholdVariableKey, nameof(ClusteringCheckinMisfireThreshold), TimeSpan.FromSeconds(5));
            ClusterHealthCheckInterval = GetSettings(configuration, Consts.ClusterHealthCheckIntervalVariableKey, nameof(ClusterHealthCheckInterval), TimeSpan.FromMinutes(1));
            ClusterPort = GetSettings<short>(configuration, Consts.ClusterPortVariableKey, nameof(ClusterPort), 12306);
            DatabaseProvider = GetSettings(configuration, Consts.DatabaseProviderVariableKey, nameof(DatabaseProvider), "Npgsql");
            ClearTraceTableOverDays = GetSettings(configuration, Consts.ClearTraceTableOverDaysVariableKey, nameof(ClearTraceTableOverDays), 365);
            ClearJobLogTableOverDays = GetSettings(configuration, Consts.ClearJobLogTableOverDaysVariableKey, nameof(ClearJobLogTableOverDays), 365);
            ClearStatisticsTablesOverDays = GetSettings(configuration, Consts.ClearStatisticsTablesOverDaysVariableKey, nameof(ClearStatisticsTablesOverDays), 365);
            SwaggerUI = GetSettings(configuration, Consts.SwaggerUIVariableKey, nameof(SwaggerUI), true);
            OpenApiUI = GetSettings(configuration, Consts.OpenApiUIVariableKey, nameof(OpenApiUI), true);
            DeveloperExceptionPage = GetSettings(configuration, Consts.DeveloperExceptionPageVariableKey, nameof(DeveloperExceptionPage), true);
            SchedulerStartupDelay = GetSettings(configuration, Consts.SchedulerStartupDelayVariableKey, nameof(SchedulerStartupDelay), TimeSpan.FromSeconds(30));
            RunDatabaseMigration = GetSettings(configuration, Consts.RunDatabaseMigrationVariableKey, nameof(RunDatabaseMigration), true);
        }

        private static void InitializeEnvironment(IConfiguration configuration)
        {
            Environment = GetSettings(configuration, Consts.EnvironmentVariableKey, nameof(Environment), Consts.ProductionEnvironment);
            Global.Environment = Environment;
            if (Environment == Consts.ProductionEnvironment)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(string.Empty.PadLeft(40, '*'));
                Console.WriteLine($"ATTENTION: Planar is running in {Consts.ProductionEnvironment} mode");
                Console.WriteLine(string.Empty.PadLeft(40, '*'));
                Console.ResetColor();
            }
        }

        private static void InitializePersistanceSpan(IConfiguration configuration)
        {
            PersistRunningJobsSpan = GetSettings<TimeSpan>(configuration, Consts.PersistRunningJobsSpanVariableKey, nameof(PersistRunningJobsSpan));

            if (PersistRunningJobsSpan == TimeSpan.Zero)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"WARNING: PersistRunningJobsSpan settings is Zero (00:00:00). Set to default value {Consts.PersistRunningJobsSpanDefaultValue}");
                Console.ResetColor();
                PersistRunningJobsSpan = Consts.PersistRunningJobsSpanDefaultValue;
            }
        }

        private static void InitializeMaxConcurrency(IConfiguration configuration)
        {
            MaxConcurrency = GetSettings<int>(configuration, Consts.MaxConcurrencyVariableKey, nameof(MaxConcurrency));

            if (MaxConcurrency == default)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"WARNING: MaxConcurrency settings is null. Set to default value {Consts.MaxConcurrencyDefaultValue}");
                Console.ResetColor();
                MaxConcurrency = Consts.MaxConcurrencyDefaultValue;
            }

            if (MaxConcurrency < 1)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"WARNING: MaxConcurrency settings is less then 1 ({MaxConcurrency}). Set to default value {Consts.MaxConcurrencyDefaultValue}");
                Console.ResetColor();
                MaxConcurrency = Consts.MaxConcurrencyDefaultValue;
            }
        }

        private static void InitializeConnectionString(IConfiguration configuration)
        {
            DatabaseConnectionString = GetSettings(configuration, Consts.ConnectionStringVariableKey, nameof(DatabaseConnectionString), string.Empty);

            if (string.IsNullOrEmpty(DatabaseConnectionString))
            {
                throw new AppSettingsException($"ERROR: database connection string could not be initialized\r\nMissing key '{nameof(DatabaseConnectionString)}' or value is empty in AppSettings.yml file and there is no environment variable '{Consts.ConnectionStringVariableKey}'");
            }
        }

        public static void TestDatabasePermission()
        {
            try
            {
                using var conn = new SqlConnection(DatabaseConnectionString);

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
                sb.AppendLine(DatabaseConnectionString);
                sb.AppendLine(seperator);
                sb.AppendLine("exception message:");
                sb.AppendLine(ex.Message);
                throw new AppSettingsException(sb.ToString());
            }
        }

        public static void TestConnectionString()
        {
            var connectionString = DatabaseConnectionString;
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
            HttpPort = GetSettings<short>(configuration, Consts.HttpPortVariableKey, nameof(HttpPort), 2306);
            HttpsPort = GetSettings<short>(configuration, Consts.HttpsPortVariableKey, nameof(HttpsPort), 2610);
            UseHttps = GetSettings(configuration, Consts.UseHttpsVariableKey, nameof(UseHttps), false);
            UseHttpsRedirect = GetSettings(configuration, Consts.UseHttpsRedirectVariableKey, nameof(UseHttpsRedirect), true);
        }

        private static void InitializeLogLevel(IConfiguration configuration)
        {
            var level = GetSettings(configuration, Consts.LogLevelVariableKey, nameof(AuthenticationMode), LogLevel.Information.ToString());
            if (Enum.TryParse<LogLevel>(level, true, out var tempLevel))
            {
                LogLevel = tempLevel;
            }
            else
            {
                LogLevel = LogLevel.Information;
            }

            Global.LogLevel = LogLevel;
        }

        private static void InitializeAuthentication(IConfiguration configuration)
        {
            const string DefaultAuthenticationSecret = "DWPVy9Xefs7JnI4mMbZMrPhp39QWpDIO";

            var mode = GetSettings(configuration, Consts.AuthenticationModeVariableKey, nameof(AuthenticationMode), AuthMode.AllAnonymous.ToString());
            AuthenticationSecret = GetSettings(configuration, Consts.AuthenticationSecretVariableKey, nameof(AuthenticationSecret), DefaultAuthenticationSecret);
            AuthenticationTokenExpire = GetSettings(configuration, Consts.AuthenticationTokenExpireVariableKey, nameof(AuthenticationTokenExpire), TimeSpan.FromMinutes(20));

            if (Enum.TryParse<AuthMode>(mode, true, out var tempMode))
            {
                AuthenticationMode = tempMode;
            }
            else
            {
                AuthenticationMode = AuthMode.AllAnonymous;
            }

            if (AuthenticationMode == AuthMode.AllAnonymous) { return; }

            if (string.IsNullOrEmpty(AuthenticationSecret))
            {
                throw new AppSettingsException($"Authentication secret must have value when authentication mode is {AuthenticationMode}");
            }

            if (AuthenticationSecret.Length < 16)
            {
                throw new AppSettingsException($"Authentication secret must have minimum length of 16 charecters. Current length is {AuthenticationSecret.Length}");
            }

            if (AuthenticationSecret.Length > 256)
            {
                throw new AppSettingsException($"Authentication secret must have maximum length of 256 charecters. Current length is {AuthenticationSecret.Length}");
            }

            if (AuthenticationTokenExpire.TotalMinutes < 1)
            {
                throw new AppSettingsException($"Authentication token expire have minimum value of 1 minute. Current length is {AuthenticationTokenExpire.TotalSeconds:N0} seconds");
            }

            AuthenticationKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AuthenticationSecret));
        }

        private static T GetSettings<T>(IConfiguration configuration, string environmentKey, string appSettingsKey, T defaultValue = default)
            where T : struct
        {
            // Environment Variable
            var property = configuration.GetValue<T?>(environmentKey);
            property ??= configuration.GetValue<T?>(appSettingsKey);
            property ??= defaultValue;

            return property.GetValueOrDefault();
        }

        private static string GetSettings(IConfiguration configuration, string environmentKey, string appSettingsKey, string defaultValue)
        {
            // Environment Variable
            var property = configuration.GetValue<string>(environmentKey);
            if (string.IsNullOrEmpty(property))
            {
                // AppSettings
                property = configuration.GetValue<string>(appSettingsKey);
            }

            if (string.IsNullOrEmpty(property))
            {
                property = defaultValue;
            }

            return property;
        }
    }
}