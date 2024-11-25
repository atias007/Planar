using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Planar.Common.Exceptions;
using System;
using System.IO;
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

    public static async Task OpenDbConnection()
    {
        switch (AppSettings.Database.ProviderName)
        {
            case DbProviders.SqlServer:
                {
                    await using var conn = new SqlConnection(AppSettings.Database.ConnectionString);
                    await conn.OpenAsync();
                    break;
                }
            case DbProviders.Sqlite:
                {
                    await using var conn = new SqliteConnection(AppSettings.Database.ConnectionString);
                    await conn.OpenAsync();
                    break;
                }
        }
    }
}