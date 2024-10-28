using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Planar.Common.Exceptions;
using System;
using System.Threading.Tasks;

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
                AppSettings.Database.ProviderHasPermissions = true;
                break;

            case DbProviders.Sqlite:
                AppSettings.Database.ProviderHasPermissions = false;
                break;
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