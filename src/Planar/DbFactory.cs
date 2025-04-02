using DbUp;
using Microsoft.Data.Sqlite;
using Planar.Common;
using RepoDb;
using Serilog;
using Serilog.Sinks.MSSqlServer;
using System;

namespace Planar;

internal static class DbFactory
{
    public static void InitializeRepoDb()
    {
        switch (AppSettings.Database.ProviderName)
        {
            case DbProviders.SqlServer:
                GlobalConfiguration.Setup().UseSqlServer();
                break;

            case DbProviders.Sqlite:
                GlobalConfiguration.Setup().UseSqlite();
                break;
        }
    }

    public static IExecuter CreateDbMigrationExecuter(DbProviders provider)
    {
        return provider switch
        {
            DbProviders.Sqlite => new DatabaseMigrations.Sqlite.Executer(),
            DbProviders.SqlServer => new DatabaseMigrations.SqlServer.Executer(),
            _ => throw new NotSupportedException($"Provider '{provider}' is not supported"),
        };
    }

    public static void AddSerilogDbSink(LoggerConfiguration config)
    {
        var sqlColumns = new ColumnOptions();
        sqlColumns.Store.Remove(StandardColumn.MessageTemplate);
        sqlColumns.Store.Remove(StandardColumn.Properties);
        sqlColumns.Store.Add(StandardColumn.LogEvent);
        sqlColumns.LogEvent.ExcludeStandardColumns = true;

        switch (AppSettings.Database.ProviderName)
        {
            case DbProviders.SqlServer:
                var sqlSink = new MSSqlServerSinkOptions
                {
                    TableName = "Trace",
                    AutoCreateSqlTable = false,
                    SchemaName = "dbo",
                };

                config.WriteTo.MSSqlServer(
                    connectionString: AppSettings.Database.ConnectionString,
                    sinkOptions: sqlSink,
                    columnOptions: sqlColumns);
                break;

            case DbProviders.Sqlite:
                var builder = new SqliteConnectionStringBuilder(AppSettings.Database.ConnectionString);
                config.WriteTo.SQLite(
                    sqliteDbPath: builder.DataSource,
                    tableName: "Trace2",
                    storeTimestampInUtc: false);

                break;
        }
    }
}