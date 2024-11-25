using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Planar.Common.Exceptions;
using Polly;
using System;
using System.Data;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using EC = Planar.Common.EnvironmentVariableConsts;

namespace Planar.Common;

internal static class DbFactory
{
    public static void SetDatabaseProvider()
    {
        if (!Enum.TryParse<DbProviders>(AppSettings.Database.Provider, out var name) || name == DbProviders.Unknown)
        {
            throw new AppSettingsException($"database provider '{AppSettings.Database.Provider}' is not supported");
        }

        AppSettings.Database.ProviderName = name;

        switch (name)
        {
            case DbProviders.SqlServer:
                if (string.IsNullOrWhiteSpace(AppSettings.Database.ConnectionString))
                {
                    throw new AppSettingsException($"ERROR: 'database connection' string could not be initialized\r\nmissing key 'connection string' or value is empty in AppSettings.yml file and there is no environment variable '{EC.ConnectionStringVariableKey}'");
                }

                AppSettings.Database.ProviderHasPermissions = true;
                AppSettings.Database.ProviderAllowClustering = true;
                break;

            case DbProviders.Sqlite:
                if (string.IsNullOrWhiteSpace(AppSettings.Database.ConnectionString))
                {
                    var dataFolder = FolderConsts.GetDataFolder(fullPath: true);
                    var filename = Path.Combine(dataFolder, "database.db");
                    var builder = new SqliteConnectionStringBuilder { DataSource = filename };
                    AppSettings.Database.ConnectionString = builder.ConnectionString;
                }

                AppSettings.Database.ProviderHasPermissions = false;
                AppSettings.Database.ProviderAllowClustering = false;
                break;
        }
    }

    public static void HandleConnectionString()
    {
        try
        {
            switch (AppSettings.Database.ProviderName)
            {
                case DbProviders.Sqlite:
                    _ = new SqliteConnectionStringBuilder(AppSettings.Database.ConnectionString);
                    break;

                case DbProviders.SqlServer:
                    var builder = new SqlConnectionStringBuilder(AppSettings.Database.ConnectionString);
                    if (builder.MultipleActiveResultSets) { return; }
                    builder.MultipleActiveResultSets = false;
                    AppSettings.Database.ConnectionString = builder.ConnectionString;
                    break;
            }
        }
        catch (Exception ex)
        {
            throw new AppSettingsException($"ERROR: 'database connection' is not valid\r\nerror message: {ex.Message}\r\nconnection string: {AppSettings.Database.ConnectionString}");
        }
    }

    public static async Task TestDatabasePermission()
    {
        if (!AppSettings.Database.ProviderHasPermissions) { return; }

        try
        {
            switch (AppSettings.Database.ProviderName)
            {
                case DbProviders.SqlServer:
                    await using (var conn = new SqlConnection(AppSettings.Database.ConnectionString))
                    {
                        await conn.OpenAsync();
                        var cmd = new CommandDefinition(
                            commandText: "admin.TestPermission",
                            commandType: CommandType.StoredProcedure);

                        conn.ExecuteAsync(cmd).Wait();
                    }
                    break;
            }

            Console.WriteLine($"    - Test database permission success");
        }
        catch (Exception ex)
        {
            var sb = new StringBuilder();
            var seperator = string.Empty.PadLeft(80, '-');
            sb.AppendLine("fail to test database permissions");
            sb.AppendLine(seperator);
            sb.AppendLine(AppSettings.Database.ConnectionString);
            sb.AppendLine(seperator);
            sb.AppendLine("exception message:");
            sb.AppendLine(ex.Message);
            throw new AppSettingsException(sb.ToString());
        }
    }

    public static async Task TestConnectionString()
    {
        var connectionString = AppSettings.Database.ConnectionString;
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new AppSettingsException("connection string is null or empty");
        }

        if (AppSettings.Database.ProviderName == DbProviders.SqlServer)
        {
            var builder = new SqlConnectionStringBuilder(connectionString) { ConnectTimeout = 3 };
            connectionString = builder.ConnectionString;
        }

        try
        {
            var counter = 1;
            await Policy.Handle<Exception>()
                .WaitAndRetryAsync(12, i => TimeSpan.FromSeconds(5))
                .ExecuteAsync(async () =>
                {
                    Console.WriteLine($"    - Attemp no {counter++} to connect to database");
                    await OpenDbConnectionInner(connectionString);
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

    private static async Task OpenDbConnectionInner(string connectionString)
    {
        switch (AppSettings.Database.ProviderName)
        {
            case DbProviders.SqlServer:
                {
                    await using var conn = new SqlConnection(connectionString);
                    await conn.OpenAsync();
                    break;
                }
            case DbProviders.Sqlite:
                {
                    await using var conn = new SqliteConnection(connectionString);
                    await conn.OpenAsync();
                    break;
                }
        }
    }
}