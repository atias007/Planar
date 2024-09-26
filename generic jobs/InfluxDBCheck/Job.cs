using Common;
using InfluxDB.Client.Core.Flux.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Planar.Job;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace InfluxDBCheck;

internal partial class Job : BaseCheckJob
{
#pragma warning disable S3251 // Implementations should be provided for "partial" methods

    static partial void CustomConfigure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context);

    static partial void CustomConfigure(ref InfluxDBServer influxServer, IConfiguration configuration);

    static partial void VetoQuery(ref InfluxQuery query);

    static partial void Finalayze(IEnumerable<InfluxQuery> endpoints);

    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
        CustomConfigure(configurationBuilder, context);

        var influxServer = new InfluxDBServer();
        CustomConfigure(ref influxServer, configurationBuilder.Build());

        if (!influxServer.IsEmpty)
        {
            var json = JsonConvert.SerializeObject(new { server = influxServer });

            // Create a JSON stream as a MemoryStream or directly from a file
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            // Add the JSON stream to the configuration builder
            configurationBuilder.AddJsonStream(stream);
        }
    }

#pragma warning restore S3251 // Implementations should be provided for "partial" methods

    private const string Template1 = "^(eq|ne|gt|ge|lt|le)\\s[-+]?\\d+(\\.\\d+)?$";
    private const string Template2 = "^(be|bi)\\s[-+]?\\d+(\\.\\d+)?\\sand\\s[-+]?\\d+(\\.\\d+)?$";
    private static readonly Regex _regex1 = new(Template1, RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));
    private static readonly Regex _regex2 = new(Template2, RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));

    public async override Task ExecuteJob(IJobExecutionContext context)
    {
        Initialize(ServiceProvider);

        var defaults = GetDefaults(Configuration);
        var server = GetServer(Configuration);
        var queries = GetQueries(Configuration, defaults);

        EffectedRows = 0;
        if (!CheckRequired(queries, "queries")) { return; }

        var proxy = new InfluxProxy(server);
        await SafeInvokeCheck(queries, q => InvokeQueryCheckInner(q, proxy));

        Finalayze(queries);
        Finalayze();
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.RegisterBaseCheck();
        services.AddSingleton<CheckIntervalTracker>();
    }

    private static Server GetServer(IConfiguration configuration)
    {
        var server = new Server(configuration);

        ValidateRequired(server.Url, "url", "server");
        ValidateRequired(server.Token, "token", "server");
        ValidateRequired(server.Organization, "organization", "server");

        ValidateUri(server.Url, "url", "server");

        return server;
    }

    private IEnumerable<InfluxQuery> GetQueries(IConfiguration configuration, Defaults defaults)
    {
        var keys = configuration.GetRequiredSection("queries");
        foreach (var item in keys.GetChildren())
        {
            var key = new InfluxQuery(item, defaults);
            VetoQuery(ref key);
            if (CheckVeto(key, "query")) { continue; }

            ValidateInfluxQuery(key);
            yield return key;
        }
    }

    private static void ValidateName(InfluxQuery query)
    {
        ValidateRequired(query.Name, "name", "queries");
        ValidateMaxLength(query.Name, 1024, "name", "queries");
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

    private static CheckException GetCheckException(InfluxQuery query, string type, double value)
    {
        if (string.IsNullOrEmpty(query.Message))
        {
            return new CheckException($"{type} condition failed for query '{query.Name}', value: {value}, condition: {query.ValueCondition?.ToLower()}");
        }
        else
        {
            var message = query.Message.Replace("{{value}}", value.ToString("N2", CultureInfo.CurrentCulture));
            return new CheckException(message);
        }
    }

    private async Task InvokeQueryCheckInner(InfluxQuery query, InfluxProxy proxy)
    {
        if (!IsIntervalElapsed(query, query.Interval))
        {
            Logger.LogInformation("skipping query '{Name}' due to its interval", query.Name);
            return;
        }

        var result = await proxy.QueryAsync(query);
        if (query.InternalValueCondition != null)
        {
            var value = GetValue(result, query.Name) ?? 0;
            var ok = query.InternalValueCondition.Evaluate(value);
            if (!ok)
            {
                throw GetCheckException(query, "value", value);
            }

            Logger.LogInformation("internal value condition '{Value} {Condition}' for check '{Name}' success", value, query.InternalValueCondition.Text, query.Name);
        }

        if (query.InternalRecordsCondition != null)
        {
            var value = GetRecords(result);
            var ok = query.InternalRecordsCondition.Evaluate(value);
            if (!ok)
            {
                throw GetCheckException(query, "records", value);
            }

            Logger.LogInformation("internal records condition '{Value} {Condition}' for check '{Name}' success", value, query.InternalRecordsCondition.Text, query.Name);
        }

        Logger.LogInformation("query check success for name '{Name}'", query.Name);
        IncreaseEffectedRows();
    }

    private static double GetRecords(List<FluxTable> tables)
    {
        if (tables.Count == 0) { return 0; }
        var table = tables[0];
        return table.Records.Count;
    }

    private static double? GetValue(List<FluxTable> tables, string name)
    {
        if (tables.Count == 0)
        {
            return null;
        }

        var table = tables[0];
        if (table.Records.Count == 0)
        {
            throw new CheckException($"could not get value from query name '{name}' (no influx records)");
        }

        var objValue = table.Records[0].GetValue();

        try
        {
            var value = Convert.ToDouble(objValue, CultureInfo.InvariantCulture);
            return value;
        }
        catch
        {
            throw new CheckException($"could not get value from query name '{name}' (value '{objValue ?? "null"}' is not numeric)");
        }
    }

    private static void ValidateInfluxQuery(InfluxQuery query)
    {
        const string root = "queries";
        ValidateBase(query, root);
        ValidateName(query);
        ValidateRequired(query.Query, $"query (name: {query.Name})", root);
        ValidateGreaterThen(query.Timeout, TimeSpan.FromSeconds(1), $"timeout (name: {query.Name})", root);
        ValidateGreaterThen(query.Interval, TimeSpan.FromMinutes(1), $"interval (name: {query.Name})", root);

        string[] values = [query.ValueCondition, query.RecordsCondition];
        string[] names = [nameof(query.ValueCondition), nameof(query.RecordsCondition)];
        ValidateAtLeastOneRequired(values, names, root);

        query.InternalRecordsCondition = ValidateCondition(query.RecordsCondition, $"records condition (name: {query.Name})");
        query.InternalValueCondition = ValidateCondition(query.ValueCondition, $"value condition (name: {query.Name})");
    }

    private static Condition? ValidateCondition(string condition, string section)
    {
        if (string.IsNullOrEmpty(condition))
        {
            return null;
        }

        var match1 = _regex1.Match(condition);
        if (match1.Success)
        {
            return new Condition(match1);
        }

        var match2 = _regex2.Match(condition);
        if (match2.Success)
        {
            return new Condition(match2);
        }

        throw new InvalidDataException($"condition '{condition}' at '{section}' section is invalid");
    }
}