using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Planar.Common;
using Planar.Service.Exceptions;
using System;
using System.Text;

namespace Planar.Service
{
    public static class AppSettings
    {
        public static int MaxConcurrency { get; set; }

        public static string ServiceName { get; set; }

        public static string InstanceId { get; set; }

        public static bool Clustering { get; set; }

        public static TimeSpan ClusteringCheckinInterval { get; set; }

        public static TimeSpan ClusteringCheckinMisfireThreshold { get; set; }

        public static TimeSpan ClusterHealthCheckInterval { get; set; }

        public static short ClusterPort { get; set; }

        public static string DatabaseConnectionString { get; set; }

        public static string DatabaseProvider { get; set; }

        public static TimeSpan PersistRunningJobsSpan { get; set; }

        public static int ClearTraceTableOverDays { get; set; }

        public static short HttpPort { get; set; }

        public static short HttpsPort { get; set; }

        public static bool UseHttpsRedirect { get; set; }

        public static bool UseHttps { get; set; }

        public static string Environment { get; set; }

        public static void Initialize(IConfiguration configuration)
        {
            InitializeEnvironment(configuration);
            InitializeConnectionString(configuration);
            InitializeMaxConcurrency(configuration);
            InitializePersistanceSpan(configuration);
            InitializePorts(configuration);

            InstanceId = GetSettings(configuration, Consts.InstanceIdVariableKey, nameof(InstanceId), "AUTO");
            ServiceName = GetSettings(configuration, Consts.ServiceNameVariableKey, nameof(ServiceName), "PlanarService");
            Clustering = GetSettings(configuration, Consts.ClusteringVariableKey, nameof(Clustering), false);
            ClusteringCheckinInterval = GetSettings(configuration, Consts.ClusteringCheckinIntervalVariableKey, nameof(ClusteringCheckinInterval), TimeSpan.FromSeconds(5));
            ClusteringCheckinMisfireThreshold = GetSettings(configuration, Consts.ClusteringCheckinMisfireThresholdVariableKey, nameof(ClusteringCheckinMisfireThreshold), TimeSpan.FromSeconds(5));
            ClusterHealthCheckInterval = GetSettings(configuration, Consts.ClusterHealthCheckIntervalVariableKey, nameof(ClusterHealthCheckInterval), TimeSpan.FromMinutes(1));
            ClusterPort = GetSettings<short>(configuration, Consts.ClusterPortVariableKey, nameof(ClusterPort), 12306);
            DatabaseProvider = GetSettings(configuration, Consts.DatabaseProviderVariableKey, nameof(DatabaseProvider), "Npgsql");
            ClearTraceTableOverDays = GetSettings(configuration, Consts.ClearTraceTableOverDaysVariableKey, nameof(ClearTraceTableOverDays), 365);
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
            DatabaseConnectionString = GetSettings(configuration, Consts.ConnectionStringVariableKey, nameof(DatabaseConnectionString));

            if (string.IsNullOrEmpty(DatabaseConnectionString))
            {
                throw new AppSettingsException($"ERROR: Database connection string could not be initialized\r\nMissing key '{nameof(DatabaseConnectionString)}' or value is empty in AppSettings.json file and there is no environment variable 'Planar_DBCONNSTR'");
            }

            CheckConnectionString(DatabaseConnectionString);
        }

        private static void CheckConnectionString(string connectionString)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                conn.Open();
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                var seperator = string.Empty.PadLeft(80, '-');
                sb.AppendLine("Fail to open connection to database using connection string");
                sb.AppendLine(seperator);
                sb.AppendLine(connectionString);
                sb.AppendLine(seperator);
                sb.AppendLine("Exception message:");
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

        private static T GetSettings<T>(IConfiguration configuration, string environmentKey, string appSettingsKey, T defaultValue = default)
            where T : struct
        {
            // Environment Variable
            var property = configuration.GetValue<T?>(environmentKey);
            if (property == null)
            {
                // AppSettings
                property = configuration.GetValue<T?>(appSettingsKey);
            }

            if (property == null)
            {
                property = defaultValue;
            }

            return property.GetValueOrDefault();
        }

        private static string GetSettings(IConfiguration configuration, string environmentKey, string appSettingsKey, string defaultValue = null)
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