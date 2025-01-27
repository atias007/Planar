using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Planar.Job;
using Seq.Api.Model.Alerting;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.Core.Tokens;

namespace SeqAlertsCheck;

internal partial class Job : BaseCheckJob
{
#pragma warning disable S3251 // Implementations should be provided for "partial" methods

    static partial void CustomConfigure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context);

    static partial void CustomConfigure(SeqServer seqServer, IConfiguration configuration);

    static partial void VetoAlert(SeqAlert alert);

    static partial void Finalayze(IEnumerable<SeqAlert> alerts);

    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
        CustomConfigure(configurationBuilder, context);

        var seqServer = new SeqServer();
        CustomConfigure(seqServer, configurationBuilder.Build());

        if (!seqServer.IsEmpty)
        {
            var json = JsonConvert.SerializeObject(new { server = seqServer });

            // Create a JSON stream as a MemoryStream or directly from a file
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            // Add the JSON stream to the configuration builder
            configurationBuilder.AddJsonStream(stream);
        }
    }

#pragma warning restore S3251 // Implementations should be provided for "partial" methods

    public async override Task ExecuteJob(IJobExecutionContext context)
    {
        Initialize(ServiceProvider);
        var server = new SeqServer(Configuration);
        ValidateServer(server);

        var defaults = GetDefaults(Configuration);
        var states = await server.GetAlerts();
        var alerts = GetAlerts(Configuration, defaults, states);

        EffectedRows = 0;

        await SafeInvokeCheck(alerts, InvokeAlertCheckInner, context.TriggerDetails);

        Finalayze(alerts);
        Finalayze();
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.RegisterSpanCheck();
    }

    protected static void ValidateServer(SeqServer server)
    {
        ValidateRequired(server.Url, "server");
        ValidateUri(server.Url, "url", "server");
    }

    private List<SeqAlert> GetAlerts(IConfiguration configuration, Defaults defaults, IEnumerable<AlertStateEntity> states)
    {
        var includes = configuration.GetSection("include alert ids").Get<List<string>>() ?? [];
        var excludes = configuration.GetSection("exclude alert ids").Get<List<string>>() ?? [];

        if (includes.Count > 0) { return GetIncludeAlerts(defaults, states, includes); }
        if (excludes.Count > 0) { return GetExcludeAlerts(defaults, states, excludes); }

        var result = new List<SeqAlert>();

        foreach (var state in states)
        {
            var alert = new SeqAlert(state, defaults);
            VetoAlert(alert);
            if (CheckVeto(alert, "alert")) { continue; }

            ValidateAlert(alert);
            result.Add(alert);
        }

        return result;
    }

    private List<SeqAlert> GetIncludeAlerts(Defaults defaults, IEnumerable<AlertStateEntity> states, IEnumerable<string> include)
    {
        var result = new List<SeqAlert>();
        foreach (var item in include)
        {
            var state = states.FirstOrDefault(a => string.Equals(a.AlertId, item, StringComparison.OrdinalIgnoreCase));
            if (state == null)
            {
                throw new CheckException($"alert from include list, with id '{item}' not found in seq");
            }

            var alert = new SeqAlert(state, defaults);
            VetoAlert(alert);
            if (CheckVeto(alert, "alert")) { continue; }

            ValidateAlert(alert);
            result.Add(alert);
        }

        return result;
    }

    private List<SeqAlert> GetExcludeAlerts(Defaults defaults, IEnumerable<AlertStateEntity> states, IEnumerable<string> excludes)
    {
        var result = new List<SeqAlert>();

        foreach (var state in states)
        {
            var isExclude = excludes.Any(e => string.Equals(e, state.AlertId, StringComparison.OrdinalIgnoreCase));
            if (isExclude)
            {
                Logger.LogInformation("alert '{AlertId}' is excluded", state.AlertId);
                continue;
            }

            var alert = new SeqAlert(state, defaults);
            VetoAlert(alert);
            if (CheckVeto(alert, "alert")) { continue; }

            ValidateAlert(alert);
            result.Add(alert);
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

    private async Task InvokeAlertCheckInner(SeqAlert alert)
    {
        var exists = await RedisFactory.Exists(key);
        key.Result.Exists = exists;
        if (key.Exists.GetValueOrDefault() && !exists)
        {
            throw new CheckException($"key '{key.Key}' is not exists");
        }

        long length = 0;
        long size = 0;
        if (key.Length > 0)
        {
            length = await RedisFactory.GetLength(key);
            key.Result.Length = length;
            Logger.LogInformation("key '{Key}' length is {Length:N0}", key.Key, length);
        }

        if (key.MemoryUsageNumber > 0)
        {
            size = await RedisFactory.GetMemoryUsage(key);
            key.Result.MemoryUsage = size;
            Logger.LogInformation("key '{Key}' size is {Size:N0} byte(s)", key.Key, size);
        }

        if (key.Length > 0 && length > key.Length)
        {
            throw new CheckException($"key '{key.Key}' length is greater then {key.Length:N0}");
        }

        if (key.MemoryUsageNumber > 0 && size > key.MemoryUsageNumber)
        {
            throw new CheckException($"key '{key.Key}' size is greater then {key.MemoryUsage:N0}");
        }

        Logger.LogInformation("redis check success for key '{Key}'", key.Key);
        IncreaseEffectedRows();
    }

    private static void ValidateHealthCheck(HealthCheck healthCheck)
    {
        ValidateGreaterThen(healthCheck.ConnectedClients, 0, "connected clients", "health check");
        ValidateGreaterThen(healthCheck.UsedMemoryNumber, 0, "used memory", "health check");
    }

    private static void ValidateAlert(SeqAlert alert)
    {
        ValidateBase(alert, $"alert ({alert.Key})");
        ValidateRequired(alert.Key, "key");
    }
}