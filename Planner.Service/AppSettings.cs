using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;

namespace Planner.Service
{
    public static class AppSettings
    {
        public static int MaxConcurrency { get; set; }

        public static string DatabaseConnectionString { get; set; }

        public static TimeSpan PersistRunningJobsSpan { get; set; }

        public static void Initialize(IConfiguration configuration)
        {
            var maxConcurrency = configuration.GetValue<int?>(Consts.MaxConcurrencyVariableKey);
            if (maxConcurrency == null)
            {
                maxConcurrency = configuration.GetValue<int?>(nameof(MaxConcurrency)).Value;
            }

            if (maxConcurrency < 1)
            {
                maxConcurrency = 1;
            }

            MaxConcurrency = maxConcurrency.GetValueOrDefault();

            // ------------------------------------------------------------------- //

            var connectionString = configuration.GetValue<string>(Consts.ConnectionStringVariableKey);

            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = configuration.GetSection("DatabaseConnectionString")?.Value;
            }

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ApplicationException("Database connection string could not be initialized\r\nMissing key 'DatabaseConnectionString' or value is empty in appsettings.json file and there is no environment variable 'PLANNER_DBCONNSTR'");
            }

            CheckConnectionString(connectionString);

            DatabaseConnectionString = connectionString;

            // ----

            var prsistanceSpan = configuration.GetValue<TimeSpan?>(Consts.PersistRunningJobsSpanVariableKey);
            if (prsistanceSpan == null)
            {
                prsistanceSpan = configuration.GetValue<TimeSpan?>(nameof(PersistRunningJobsSpan)).Value;
            }

            if (prsistanceSpan.GetValueOrDefault() == TimeSpan.Zero)
            {
                prsistanceSpan = TimeSpan.FromMinutes(5);
            }

            PersistRunningJobsSpan = prsistanceSpan.GetValueOrDefault();
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
                throw new ApplicationException($"Fail to open connection to database using connection string '{connectionString}'", ex);
            }
        }
    }
}