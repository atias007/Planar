using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Planar.Service.Exceptions;
using System;
using System.Text;

namespace Planar.Service
{
    public static class AppSettings
    {
        public static int MaxConcurrency { get; set; }

        public static string DatabaseConnectionString { get; set; }

        public static TimeSpan PersistRunningJobsSpan { get; set; }

        public static int HttpPort { get; set; }

        public static int HttpsPort { get; set; }

        public static bool UseHttpsRedirect { get; set; }

        public static bool UseHttps { get; set; }

        public static void Initialize(IConfiguration configuration)
        {
            InitializeConnectionString(configuration);
            InitializeMaxConcurrency(configuration);
            InitializePersistanceSpan(configuration);
            InitializePorts(configuration);
        }

        private static void InitializePersistanceSpan(IConfiguration configuration)
        {
            // Environment Variable
            var prsistanceSpan = configuration.GetValue<TimeSpan?>(Consts.PersistRunningJobsSpanVariableKey);
            if (prsistanceSpan == null)
            {
                // AppSettings
                prsistanceSpan = configuration.GetValue<TimeSpan?>(nameof(PersistRunningJobsSpan));
            }

            if (prsistanceSpan == null)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"WARNING: PersistRunningJobsSpan settings is null. Set to default value {Consts.PersistRunningJobsSpanDefaultValue}");
                Console.ResetColor();
                prsistanceSpan = Consts.PersistRunningJobsSpanDefaultValue;
            }

            if (prsistanceSpan.GetValueOrDefault() == TimeSpan.Zero)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"WARNING: PersistRunningJobsSpan settings is Zero (00:00:00). Set to default value {Consts.PersistRunningJobsSpanDefaultValue}");
                Console.ResetColor();
                prsistanceSpan = Consts.PersistRunningJobsSpanDefaultValue;
            }

            PersistRunningJobsSpan = prsistanceSpan.GetValueOrDefault();
        }

        private static void InitializeMaxConcurrency(IConfiguration configuration)
        {
            // Environment Variable
            var maxConcurrency = configuration.GetValue<int?>(Consts.MaxConcurrencyVariableKey);
            if (maxConcurrency == null)
            {
                // AppSettings
                maxConcurrency = configuration.GetValue<int?>(nameof(MaxConcurrency));
            }

            if (maxConcurrency == null)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"WARNING: MaxConcurrency settings is null. Set to default value {Consts.MaxConcurrencyDefaultValue}");
                Console.ResetColor();
                maxConcurrency = Consts.MaxConcurrencyDefaultValue;
            }

            if (maxConcurrency < 1)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"WARNING: MaxConcurrency settings is less then 1 ({maxConcurrency}). Set to default value {Consts.MaxConcurrencyDefaultValue}");
                Console.ResetColor();
                maxConcurrency = Consts.MaxConcurrencyDefaultValue;
            }

            MaxConcurrency = maxConcurrency.GetValueOrDefault();
        }

        private static void InitializeConnectionString(IConfiguration configuration)
        {
            // Environment Variable
            var connectionString = configuration.GetValue<string>(Consts.ConnectionStringVariableKey);
            if (string.IsNullOrEmpty(connectionString))
            {
                // AppSettings
                connectionString = configuration.GetValue<string>("DatabaseConnectionString");
            }

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new AppSettingsException("ERROR: Database connection string could not be initialized\r\nMissing key 'DatabaseConnectionString' or value is empty in appsettings.json file and there is no environment variable 'Planar_DBCONNSTR'");
            }

            CheckConnectionString(connectionString);

            DatabaseConnectionString = connectionString;
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
            HttpPort = GetSettings(configuration, Consts.HttpPortVariableKey, "HttpPort", 2306);
            HttpsPort = GetSettings(configuration, Consts.HttpsPortVariableKey, "HttpsPort", 2610);
            UseHttps = GetSettings(configuration, Consts.UseHttpsVariableKey, "UseHttps", false);
            UseHttpsRedirect = GetSettings(configuration, Consts.UseHttpsRedirectVariableKey, "UseHttpsRedirect", true);
            ////// Environment Variable
            ////var httpPort = configuration.GetValue<int?>(Consts.HttpPortVariableKey);
            ////if (httpPort == null)
            ////{
            ////    // AppSettings
            ////    httpPort = configuration.GetValue<int?>("HttpPort");
            ////}

            ////if (httpPort == null)
            ////{
            ////    httpPort = 2306;
            ////}

            ////HttpPort = httpPort.GetValueOrDefault();

            ////// Environment Variable
            ////var httpsPort = configuration.GetValue<int?>(Consts.HttpsPortVariableKey);
            ////if (httpsPort == null)
            ////{
            ////    // AppSettings
            ////    httpsPort = configuration.GetValue<int?>("HttpsPort");
            ////}

            ////if (httpsPort == null)
            ////{
            ////    httpsPort = 2610;
            ////}

            ////HttpsPort = httpsPort.GetValueOrDefault();

            ////// Environment Variable
            ////var useHttpsRedirect = configuration.GetValue<bool?>(Consts.UseHttpsRedirectVariableKey);
            ////if (useHttpsRedirect == null)
            ////{
            ////    // AppSettings
            ////    useHttpsRedirect = configuration.GetValue<bool?>("UseHttpsRedirect");
            ////}

            ////if (useHttpsRedirect == null)
            ////{
            ////    useHttpsRedirect = true;
            ////}

            ////UseHttpsRedirect = useHttpsRedirect.GetValueOrDefault();
        }

        private static T GetSettings<T>(IConfiguration configuration, string environmentKey, string appSettingsKey, T defaultValue)
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
    }
}