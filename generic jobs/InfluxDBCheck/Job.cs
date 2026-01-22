using Common;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core.Flux.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Planar.Job;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using YamlDotNet.Core.Tokens;

namespace InfluxDBCheck;

internal partial class Job : BaseCheckJob
{
#pragma warning disable S3251 // Implementations should be provided for "partial" methods

    partial void CustomConfigure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context);

    partial void CustomConfigure(InfluxDBServer influxServer, IConfiguration configuration);

    partial void VetoQuery(InfluxQuery query);

    partial void Finalayze(FinalayzeDetails<IEnumerable<InfluxQuery>> details);

    partial void OnFail<T>(T entity, Exception ex, int? retryCount) where T : BaseDefault, ICheckElement;

    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
        CustomConfigure(configurationBuilder, context);

        var influxServer = new InfluxDBServer();
        CustomConfigure(influxServer, configurationBuilder.Build());

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
        var keys = GetKeys(context);
        var queries = GetQueries(Configuration, defaults, keys);

        await SetEffectedRowsAsync(0);
        if (!CheckRequired(queries, "queries")) { return; }

        var proxy = new InfluxProxy(server);
        await SafeInvokeCheck(queries, q => InvokeQueryCheckInner(q, proxy), context.TriggerDetails);

        var details = GetFinalayzeDetails(queries.AsEnumerable());
        Finalayze(details);
        Finalayze();
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.RegisterSpanCheck();
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

    private List<InfluxQuery> GetQueries(IConfiguration configuration, Defaults defaults)
    {
        var queries = configuration.GetRequiredSection("queries");
        var result = new List<InfluxQuery>();

        foreach (var item in queries.GetChildren())
        {
            var query = new InfluxQuery(item, defaults);
            VetoQuery(query);
            if (CheckVeto(query, "query")) { continue; }

            ValidateInfluxQuery(query);
            result.Add(query);
        }

        ValidateRequired(result, "queries");
        ValidateDuplicateNames(result, "queries");

        return result;
    }

    private List<InfluxQuery> GetQueries(IConfiguration configuration, Defaults defaults, IEnumerable<string>? keys)
    {
        if (keys == null || !keys.Any()) { return GetQueries(configuration, defaults); }

        var result = new List<InfluxQuery>();
        var queries = configuration.GetRequiredSection("queries");

        foreach (var item in queries.GetChildren())
        {
            var query = new InfluxQuery(item, defaults);
            query.ForceRun();
            ValidateInfluxQuery(query);
            result.Add(query);
        }

        ValidateRequired(result, "queries");
        ValidateDuplicateNames(result, "queries");

        return result;
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
        var result = await proxy.QueryAsync(query);
        if (query.InternalValueCondition != null)
        {
            var value = GetValue(result, query.Name) ?? 0;
            query.Result.QueryValue = value;
            var ok = query.InternalValueCondition.Evaluate(value);
            if (!ok)
            {
                throw GetCheckException(query, "value", value);
            }

            query.ResultMessage = $"value condition '{value} {query.InternalValueCondition.Text}' for check '{query.Name}' success";
            Logger.LogInformation("value condition '{Value} {Condition}' for check '{Name}' success", value, query.InternalValueCondition.Text, query.Name);
        }

        if (query.InternalRecordsCondition != null)
        {
            var value = GetRecordsCount(result);
            query.Result.QueryRecordsCount = value;
            var ok = query.InternalRecordsCondition.Evaluate(value);
            if (!ok)
            {
                var ex = GetCheckException(query, "records", value);
                query.ResultMessage = ex.Message;
                throw ex;
            }

            query.ResultMessage = $"records condition '{value} {query.InternalRecordsCondition.Text}' for check '{query.Name}' success";
            Logger.LogInformation("records condition '{Value} {Condition}' for check '{Name}' success", value, query.InternalRecordsCondition.Text, query.Name);
        }

        Logger.LogInformation("query check success for name '{Name}'", query.Name);
        await IncreaseEffectedRowsAsync();
    }

    protected override void BaseOnFail<T>(T entity, Exception ex, int? retryCount)
    {
        OnFail(entity, ex, retryCount);
    }

    private static int GetRecordsCount(List<FluxTable> tables)
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