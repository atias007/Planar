using Common;
using InfluxDB.Client.Core.Flux.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using System.Text.RegularExpressions;

namespace InfluxDBCheck;

internal class Job : BaseCheckJob
{
    private const string template1 = "^(eq|ne|gt|ge|lt|le)\\s[-+]?\\d+(\\.\\d+)?$";
    private const string template2 = "^(be|bi)\\s[-+]?\\d+(\\.\\d+)?\\sand\\s[-+]?\\d+(\\.\\d+)?$";
    private static readonly Regex _regex1 = new(template1, RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));
    private static readonly Regex _regex2 = new(template2, RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));

    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
    }

    public async override Task ExecuteJob(IJobExecutionContext context)
    {
        Initialize(ServiceProvider);

        var defaults = GetDefaults(Configuration);
        var server = new Server(Configuration);
        var queries = GetQueries(Configuration, defaults);
        ValidateRequired(queries, "queries");

        EffectedRows = 0;

        var proxy = new InfluxProxy(server);
        await SafeInvokeCheck(queries, q => InvokeQueryCheckInner(q, proxy));

        Finilayze();
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.RegisterBaseCheck();
    }

    private static void FillDefaults(InfluxQuery redisKey, Defaults defaults)
    {
        // Fill Defaults
        redisKey.Name ??= string.Empty;
        redisKey.Name = redisKey.Name.Trim();
        FillBase(redisKey, defaults);
    }

    private static IEnumerable<InfluxQuery> GetQueries(IConfiguration configuration, Defaults defaults)
    {
        var keys = configuration.GetRequiredSection("queries");
        foreach (var item in keys.GetChildren())
        {
            var key = new InfluxQuery(item);
            FillDefaults(key, defaults);
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

    private async Task InvokeQueryCheckInner(InfluxQuery query, InfluxProxy proxy)
    {
        if (!query.Active)
        {
            Logger.LogInformation("skipping inactive query '{Query}'", query.Query);
            return;
        }

        var result = await proxy.QueryAsync(query);
        if (query.InternalValueCondition != null)
        {
            var value = GetValue(result, query.Name);
            var ok = query.InternalValueCondition.Evaluate(value);
            if (!ok)
            {
                throw new CheckException($"value condition failed for query name '{query.Name}', value: {value}, condition: {query.ValueCondition?.ToLower()}");
            }
        }

        if (query.InternalRecordsCondition != null)
        {
            var value = GetRecords(result);
            var ok = query.InternalRecordsCondition.Evaluate(value);
            if (!ok)
            {
                throw new CheckException($"records condition failed for query name '{query.Name}', value: {value}, condition: {query.RecordsCondition?.ToLower()}");
            }
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

    private static double GetValue(List<FluxTable> tables, string name)
    {
        if (tables.Count == 0)
        {
            throw new CheckException($"could not get value from query name '{name}' (no influx tables");
        }

        var table = tables[0];
        if (table.Records.Count == 0)
        {
            throw new CheckException($"could not get value from query name '{name}' (no influx records");
        }

        var objValue = table.Records[0].GetValue();
        var value = objValue as double? ??
            throw new CheckException($"could not get value from query name '{name}' (value '{objValue}' is not numeric)");

        return value;
    }

    private static void ValidateInfluxQuery(InfluxQuery query)
    {
        const string root = "queries";
        ValidateBase(query, root);
        ValidateName(query);
        ValidateRequired(query.Query, $"query (name: {query.Name})", root);
        ValidateRequired(query.Message, $"message (name: {query.Name})", root);
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