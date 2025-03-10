using Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Planar.Job;
using Sql;
using System.Text;

namespace SqlTableRetention;

internal partial class Job : BaseCheckJob
{
#pragma warning disable S3251 // Implementations should be provided for "partial" methods

    static partial void CustomConfigure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context);

    static partial void CustomConfigure(List<SqlConnectionString> connectionStrings, IConfiguration configuration);

    static partial void VetoTable(Table table);

    static partial void Finalayze(FinalayzeDetails<IEnumerable<Table>> details);

#pragma warning restore S3251 // Implementations should be provided for "partial" methods

    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
        CustomConfigure(configurationBuilder, context);

        var connectionStrings = new List<SqlConnectionString>();
        CustomConfigure(connectionStrings, configurationBuilder.Build());

        if (connectionStrings.Count > 0)
        {
            var dic = connectionStrings.ToDictionary(k => k.Name, e => e.ConnectionString);
            var json = JsonConvert.SerializeObject(new { connectionStrings = dic });

            // Create a JSON stream as a MemoryStream or directly from a file
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            // Add the JSON stream to the configuration builder
            configurationBuilder.AddJsonStream(stream);
        }
    }

    public override async Task ExecuteJob(IJobExecutionContext context)
    {
        Initialize(ServiceProvider);

        EffectedRows = 0;
        var defaults = GetDefaults(Configuration);
        var connStrings = GetConnectionStrings(Configuration);
        var tables = GetTables(Configuration, connStrings, defaults);
        ValidateRequired(tables, "tables");
        ValidateDuplicateNames(tables, "tables");

        await SafeInvokeOperation(tables, InvokeTableRerentionInner, context.TriggerDetails);

        var details = GetFinalayzeDetails(tables.AsEnumerable());
        Finalayze(details);
        Finalayze();
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.RegisterSpanCheck();
    }

    private Defaults GetDefaults(IConfiguration configuration)
    {
        var empty = Defaults.Empty;
        var section = configuration.GetSection("defaults");
        if (section == null)
        {
            Logger.LogWarning("no defaults section found on settings file. set job factory defaults");
            return empty;
        }

        var result = new Defaults(section);
        ValidateBase(result, "defaults");

        return result;
    }

    private async Task InvokeTableRerentionInner(Table table)
    {
        var timeout = table.Timeout;
        var query = GetQuery(table);
        Logger.LogDebug("retention '{Name}' executed with query: {Query}", table.Name, query);
        using var connection = new SqlConnection(table.ConnectionString);
        using var cmd = new SqlCommand(query, connection)
        {
            CommandTimeout = (int)timeout.TotalSeconds
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

    private IEnumerable<Table> GetTables(IConfiguration configuration, Dictionary<string, string> connectionStrings, Defaults defaults)
    {
        var section = configuration.GetRequiredSection("tables");
        foreach (var item in section.GetChildren())
        {
            var key = new Table(item, defaults);

            VetoTable(key);
            if (CheckVeto(key, "table")) { continue; }

            key.ConnectionString = connectionStrings.GetValueOrDefault(key.ConnectionStringName);
            ValidateTable(key);
            yield return key;
        }
    }
}