using Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Planar.Job;
using Sql;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SqlQueryCheck;

internal partial class Job : BaseCheckJob
{
#pragma warning disable S3251 // Implementations should be provided for "partial" methods

    partial void CustomConfigure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context);

    partial void CustomConfigure(ref List<SqlConnectionString> connectionStrings, IConfiguration configuration);

    partial void VetoQuery(ref CheckQuery query);

    partial void FillConnectionStrings(Dictionary<string, string> connectionStrings, IConfiguration configuration, string environment);

    partial void Finalayze(FinalayzeDetails<IEnumerable<CheckQuery>> details);

    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
        CustomConfigure(configurationBuilder, context);

        var connectionStrings = new List<SqlConnectionString>();
        CustomConfigure(ref connectionStrings, configurationBuilder.Build());

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

#pragma warning restore S3251 // Implementations should be provided for "partial" methods

    public override async Task ExecuteJob(IJobExecutionContext context)
    {
        Initialize(ServiceProvider);

        var defaults = GetDefaults(Configuration);
        var names = GetConnectionStringNames(Configuration);
        var connStrings = GetConnectionStrings(Configuration, names);
        FillConnectionStrings(connStrings, Configuration, context.Environment);
        var queries = GetQueries(Configuration, defaults, connStrings);
        ValidateRequired(queries, "queries");
        ValidateDuplicateNames(queries, "queries");
        EffectedRows = 0;
        await SafeInvokeCheck(queries, InvokeQueryCheckInner, context.TriggerDetails);

        var details = GetFinalayzeDetails(queries.AsEnumerable());
        Finalayze(details);
        Finalayze();
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.RegisterSpanCheck();
    }

    private async Task InvokeQueryCheckInner(CheckQuery checkQuery)
    {
        using var connection = new SqlConnection(checkQuery.ConnectionString);
        using var cmd = new SqlCommand(checkQuery.Query, connection)
        {
            CommandTimeout = (int)checkQuery.Timeout.TotalSeconds
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

            checkQuery.ResultMessage = message;
            AddCheckException(new CheckException(message));
        }
        else
        {
            checkQuery.ResultMessage = $"query {checkQuery.Name} query executed successfully";
            Logger.LogInformation("query '{Name}' executed successfully", checkQuery.Name);
            IncreaseEffectedRows();
        }
    }

    private static string Replace(string message, SqlDataReader reader)
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
                    var value = Convert.ToString(reader[phInner], CultureInfo.InvariantCulture);
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
                var value = Convert.ToString(reader[intPhInner.GetValueOrDefault()], CultureInfo.InvariantCulture);
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

        if (string.IsNullOrWhiteSpace(checkQuery.ConnectionString))
        {
            throw new InvalidDataException($"connection string with name '{checkQuery.ConnectionStringName}' not found");
        }
    }

    private List<CheckQuery> GetQueries(IConfiguration configuration, Defaults defaults, Dictionary<string, string> connectionStrings)
    {
        var queries = new List<CheckQuery>();
        var section = configuration.GetRequiredSection("queries");
        foreach (var item in section.GetChildren())
        {
            var key = new CheckQuery(item, defaults);
            key.ConnectionString = connectionStrings.GetValueOrDefault(key.ConnectionStringName);

            VetoQuery(ref key);
            if (CheckVeto(key, "query")) { continue; }
            ValidateCheckQuery(key);
            queries.Add(key);
        }

        return queries;
    }

    private static List<string> GetConnectionStringNames(IConfiguration configuration)
    {
        var result = new List<string>();
        var section = configuration.GetRequiredSection("queries");
        foreach (var item in section.GetChildren())
        {
            var name = item.GetValue<string>("connection string name");
            if (!string.IsNullOrWhiteSpace(name))
            {
                result.Add(name);
            }
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