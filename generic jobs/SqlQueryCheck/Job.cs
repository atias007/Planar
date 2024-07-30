using Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using System.Data;
using System.Text.RegularExpressions;

namespace SqlQueryCheck;

internal partial class Job : BaseCheckJob
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
        EffectedRows = 0;
        await SafeInvokeCheck(queries, InvokeQueryCheckInner);

        Finilayze();
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

        if (!IsIntervalElapsed(checkQuery, checkQuery.Interval))
        {
            Logger.LogInformation("skipping query '{Name}' due to its interval", checkQuery.Name);
            return;
        }

        using var connection = new SqlConnection(checkQuery.ConnectionString);
        using var cmd = new SqlCommand(checkQuery.Query, connection)
        {
            CommandTimeout = (int)checkQuery.Timeout.TotalMilliseconds
        };

        await connection.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);

        var hasData = await reader.ReadAsync();
        if (hasData)
        {
            var message =
                string.IsNullOrWhiteSpace(checkQuery.Message) ?
                $"{checkQuery.Name} query failed" :
                Replace(checkQuery.Message, reader);

            AddCheckException(new CheckException(message));
        }
        else
        {
            Logger.LogInformation("query '{Name}' executed successfully", checkQuery.Name);
            IncreaseEffectedRows();
        }
    }

    private static string Replace(string message, IDataReader reader)
    {
        var regex = Placeholder();
        var matches = regex.Matches(message);
        if (matches.Count == 0) { return message; }

        foreach (Match item in matches.Cast<Match>())
        {
            if (item.Groups.Count == 0) { continue; }
            var ph = item.Groups[0].Value;
            var phInner = ph[2..^2];
            var intPhInner = IsInt(phInner);
            if (intPhInner == null)
            {
                try
                {
                    var value = Convert.ToString(reader[phInner]);
                    message = message.Replace(ph, value);
                }
                catch (Exception)
                {
                    // *** DO NOTHING *** //
                }
            }
            else
            {
                if (reader.FieldCount >= intPhInner) { continue; }
                var value = Convert.ToString(reader[intPhInner.GetValueOrDefault()]);
                message = message.Replace(ph, value);
            }
        }

        return message;
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

    private static void ValidateCheckQuery(CheckQuery checkQuery)
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

    private static IEnumerable<CheckQuery> GetQueries(IConfiguration configuration, Defaults defaults, Dictionary<string, string> connectionStrings)
    {
        var section = configuration.GetRequiredSection("queries");
        foreach (var item in section.GetChildren())
        {
            var key = new CheckQuery(item, defaults);
            key.ConnectionString = connectionStrings.GetValueOrDefault(key.ConnectionStringName);
            ValidateCheckQuery(key);
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

    [GeneratedRegex("{{[0-9]+}}|{{\\w+}}")]
    private static partial Regex Placeholder();
}