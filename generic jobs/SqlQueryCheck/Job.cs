using Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using System.Globalization;

namespace SqlQueryCheck;

internal class Job : BaseCheckJob
{
    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
    }

    public override async Task ExecuteJob(IJobExecutionContext context)
    {
        Initialize(ServiceProvider);
        var defaults = GetDefaults(Configuration);
        var connStrings = GetConnectionStrings(Configuration);
        var queries = GetQueries(Configuration, defaults, connStrings);
        ValidateRequired(queries, "queries");
        ValidateDuplicateNames(queries, "queries");

        var tasks = SafeInvokeCheck(queries, InvokeQueryCheckInner);
        await Task.WhenAll(tasks);

        CheckAggragateException();
        HandleCheckExceptions();
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.RegisterBaseCheck();
        services.AddSingleton<CheckIntervalTracker>();
    }

    private async Task InvokeQueryCheckInner(CheckQuery checkQuery)
    {
        if (!checkQuery.Active)
        {
            Logger.LogInformation("skipping inactive query '{Name}'", checkQuery.Name);
            return;
        }

        if (!ValidateCheckQuery(checkQuery)) { return; }

        if (checkQuery.Interval.HasValue)
        {
            var tracker = ServiceProvider.GetRequiredService<CheckIntervalTracker>();
            var lastSpan = tracker.LastRunningSpan(checkQuery);
            if (lastSpan > TimeSpan.Zero && lastSpan < checkQuery.Interval)
            {
                Logger.LogInformation("skipping query '{Name}' due to its interval", checkQuery.Name);
                return;
            }
        }

        var timeout = checkQuery.Timeout ?? TimeSpan.FromSeconds(30);
        using var connection = new SqlConnection(checkQuery.ConnectionString);
        using var cmd = new SqlCommand(checkQuery.Query, connection)
        {
            CommandTimeout = (int)timeout.TotalMilliseconds
        };

        await connection.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();
        object? result = null;
        int count = 0;

        while (await reader.ReadAsync())
        {
            result ??= reader[0];
            count++;
        }

        if (count > 0)
        {
            var message =
                string.IsNullOrWhiteSpace(checkQuery.Message) ?
                $"'{checkQuery.Name}' query failed with total {count} row(s)" :
                checkQuery.Message;

            message = message
                .Replace("{{count}}", count.ToString(CultureInfo.CurrentCulture))
                .Replace("{{result}}", result?.ToString());

            AddCheckException(new CheckException(message));
        }
        else
        {
            Logger.LogInformation("query '{Name}' executed successfully", checkQuery.Name);
        }
    }

    private bool ValidateCheckQuery(CheckQuery checkQuery)
    {
        try
        {
            ValidateBase(checkQuery, $"key ({checkQuery.Key})");
            ValidateRequired(checkQuery.Name, "name", "queries");
            ValidateRequired(checkQuery.ConnectionStringName, "connection string name", "queries");
            ValidateRequired(checkQuery.Query, "query", "queries");
            ValidateGreaterThen(checkQuery.Timeout, TimeSpan.FromSeconds(1), "timeout", "queries");
            ValidateGreaterThen(checkQuery.Interval, TimeSpan.FromMinutes(1), "interval", "queries");

            if (string.IsNullOrWhiteSpace(checkQuery.ConnectionString))
            {
                throw new InvalidDataException($"connection string with name '{checkQuery.ConnectionStringName}' not found");
            }
        }
        catch (Exception ex)
        {
            AddAggregateException(ex);
            return false;
        }

        return true;
    }

    private static IEnumerable<CheckQuery> GetQueries(IConfiguration configuration, Defaults defaults, Dictionary<string, string> connectionStrings)
    {
        var section = configuration.GetRequiredSection("queries");
        foreach (var item in section.GetChildren())
        {
            var key = new CheckQuery(item);
            FillBase(key, defaults);
            key.ConnectionString = connectionStrings.GetValueOrDefault(key.ConnectionStringName);
            yield return key;
        }
    }

    private static Dictionary<string, string> GetConnectionStrings(IConfiguration configuration)
    {
        var section = configuration.GetRequiredSection("connection strings");
        var result = new Dictionary<string, string>();
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

        return result;
    }

    private Defaults GetDefaults(IConfiguration configuration)
    {
        var section = GetDefaultSection(configuration, Logger);
        if (section == null)
        {
            return Defaults.Empty;
        }

        var result = new Defaults(section);
        ValidateBase(result, "defaults");
        return result;
    }
}