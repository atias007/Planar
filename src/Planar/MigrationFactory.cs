using DbUp;
using System;

namespace Planar;

public static class MigrationFactory
{
    public static IExecuter CreateExecuter(string provider)
    {
        provider = provider?.ToLower() ?? string.Empty;
        switch (provider)
        {
            case "sqlite":
                return new DatabaseMigrations.Sqlite.Executer();

            case "sqlserver":
                return new DatabaseMigrations.SqlServer.Executer();

            default:
                throw new NotSupportedException($"Provider '{provider}' is not supported");
        }
    }
}