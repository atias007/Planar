using Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using System.Text.RegularExpressions;

namespace SqlTableRetention;

internal partial class Job : BaseCheckJob
{
    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
    }

    public override async Task ExecuteJob(IJobExecutionContext context)
    {
        EffectedRows = 0;
        Initialize(ServiceProvider);
        var connStrings = GetConnectionStrings(Configuration);
        var tables = GetTables(Configuration, connStrings);
        ValidateRequired(tables, "tables");
        ValidateDuplicateNames(tables, "tables");

        var tasks = SafeInvokeOperation(tables, InvokeTableRerentionInner);
        await Task.WhenAll(tasks);

        CheckAggragateException();
        HandleCheckExceptions();
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.RegisterBaseCheck();
        services.AddSingleton<CheckIntervalTracker>();
    }

    private async Task InvokeTableRerentionInner(Table table)
    {
        if (!table.Active)
        {
            Logger.LogInformation("skipping inactive table '{Name}'", table.Name);
            return;
        }

        var timeout = table.Timeout;
        var query = GetQuery(table);
        Logger.LogDebug("retention '{Name}' executed with query: {Query}", table.Name, query);
        using var connection = new SqlConnection(table.ConnectionString);
        using var cmd = new SqlCommand(query, connection)
        {
            CommandTimeout = (int)timeout.TotalMilliseconds
        };

        await connection.OpenAsync();
        var count = await cmd.ExecuteNonQueryAsync();
        Logger.LogInformation("retention '{Name}' executed successfully with {Count} effected row(s)", table.Name, count);
        EffectedRows += count;
    }

    private static string GetQuery(Table table)
    {
        var tableName = GetTableName(table);
        string query;
        if (string.IsNullOrWhiteSpace(table.Condition))
        {
            query = $"TUNCATE TABLE {tableName}";
        }
        else
        {
            query = $"""
            DECLARE @batchSize INT = {table.BatchSize};

            WHILE EXISTS (SELECT TOP 1 1 FROM {tableName} WHERE {table.Condition})
            BEGIN
              DELETE TOP (@batchSize) FROM {tableName}
              WHERE {table.Condition};
            END
            """;
        }

        return query;
    }

    private static string GetTableName(Table table)
    {
        var schema = WrapSqlElement(table.Schema);
        var tableName = WrapSqlElement(table.TableName);
        return $"{schema}.{tableName}";
    }

    private static string WrapSqlElement(string element)
    {
        if (element[0] != '[') { element = $"[{element}"; }
        if (element[^1] != ']') { element = $"{element}]"; }
        return element;
    }

    public static int? IsInt(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (!int.TryParse(value, out var result)) { return null; }
        if (result < 0) { return null; }
        return result;
    }

    private static void ValidateTable(Table table)
    {
        ValidateRequired(table.Name, "name", "tables");
        ValidateRequired(table.ConnectionStringName, "connection string name", "tables");
        ValidateRequired(table.Schema, "schema", "tables");
        ValidateRequired(table.TableName, "table name", "tables");
        ValidateGreaterThen(table.Timeout, TimeSpan.FromSeconds(1), "timeout", "tables");
        ValidateGreaterThenOrEquals(table.BatchSize, 1_000, "batch size", "tables");
        ValidateLessThenOrEquals(table.BatchSize, 50_000, "batch size", "tables");

        if (string.IsNullOrWhiteSpace(table.ConnectionString))
        {
            throw new InvalidDataException($"connection string with name '{table.ConnectionStringName}' not found");
        }
    }

    private static IEnumerable<Table> GetTables(IConfiguration configuration, Dictionary<string, string> connectionStrings)
    {
        var section = configuration.GetRequiredSection("tables");
        foreach (var item in section.GetChildren())
        {
            var key = new Table(item);
            key.ConnectionString = connectionStrings.GetValueOrDefault(key.ConnectionStringName);
            ValidateTable(key);
            yield return key;
        }
    }

    private Dictionary<string, string> GetConnectionStrings(IConfiguration configuration)
    {
        var result = new Dictionary<string, string>();

        var section = configuration.GetSection("connection strings");
        if (section.Exists())
        {
            foreach (var item in section.GetChildren())
            {
                if (string.IsNullOrWhiteSpace(item.Key))
                {
                    throw new InvalidDataException("connection string has invalid null or empty key");
                }

                if (string.IsNullOrWhiteSpace(item.Value))
                {
                    throw new InvalidDataException($"connection string with key '{item.Key}' has no value");
                }

                result.TryAdd(item.Key, item.Value);
            }
        }

        section = Configuration.GetSection("connection strings");
        if (section.Exists())
        {
            foreach (var item in section.GetChildren())
            {
                if (string.IsNullOrWhiteSpace(item.Key)) { continue; }
                if (string.IsNullOrWhiteSpace(item.Value)) { continue; }
                result.TryAdd(item.Key, item.Value);
            }
        }

        section = Configuration.GetSection("ConnectionStrings");
        if (section.Exists())
        {
            foreach (var item in section.GetChildren())
            {
                if (string.IsNullOrWhiteSpace(item.Key)) { continue; }
                if (string.IsNullOrWhiteSpace(item.Value)) { continue; }
                result.TryAdd(item.Key, item.Value);
            }
        }

        return result;
    }

    [GeneratedRegex("{{[0-9]+}}|{{\\w+}}")]
    private static partial Regex Placeholder();
}