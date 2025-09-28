using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Planar.Job;
using System.Text;

namespace SeqAlertsCheck;

internal partial class Job : BaseCheckJob
{
#pragma warning disable S3251 // Implementations should be provided for "partial" methods

    partial void CustomConfigure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context);

    partial void CustomConfigure(SeqServer seqServer, IConfiguration configuration);

    partial void VetoAlert(SeqAlert alert);

    partial void Finalayze(FinalayzeDetails<IEnumerable<SeqAlert>> details);

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
        MapTitles(Configuration, alerts);

        await SetEffectedRowsAsync(0);

        await SafeInvokeCheck(alerts, InvokeAlertCheckInner, context.TriggerDetails);

        var details = GetFinalayzeDetails(alerts.AsEnumerable());
        Finalayze(details);
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

    private static void MapTitles(IConfiguration configuration, List<SeqAlert> alerts)
    {
        var section = configuration.GetSection("alert titles");
        var titles = section.Get<Dictionary<string, string>>() ?? [];
        foreach (var item in titles)
        {
            var alert = alerts.FirstOrDefault(a => a.Key == item.Key);
            if (alert == null) { continue; }
            alert.Title = item.Value;
        }
    }

    private List<SeqAlert> GetAlerts(IConfiguration configuration, Defaults defaults, IEnumerable<AlertState> states)
    {
        var includes = configuration.GetSection("include alert ids").Get<List<string>>() ?? [];
        var excludes = configuration.GetSection("exclude alert ids").Get<List<string>>() ?? [];

        if (includes.Count > 0 && excludes.Count > 0)
        {
            throw new InvalidOperationException("both include and exclude alert ids are not allowed");
        }

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

    private List<SeqAlert> GetIncludeAlerts(Defaults defaults, IEnumerable<AlertState> states, IEnumerable<string> include)
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

    private List<SeqAlert> GetExcludeAlerts(Defaults defaults, IEnumerable<AlertState> states, IEnumerable<string> excludes)
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
        const string shared = "(shared)";

        await IncreaseEffectedRowsAsync();

        var state = alert.AlertState;
        var title = alert.Title;
        var owner = string.IsNullOrWhiteSpace(state.OwnerId) ? shared : state.OwnerId;
        if (!state.IsFailing)
        {
            alert.ResultMessage = $"alert: {title}, id: {state.AlertId}, owner: {owner}, is in ok state";
            Logger.LogInformation("alert: {Title}, id: {AlertId}, owner: {Owner}, is in ok state", title, state.AlertId, owner);
            return;
        }

        if (state.SuppressedUntil.HasValue)
        {
            if (state.SuppressedUntil.Value >= DateTime.UtcNow)
            {
                alert.ResultMessage = $"alert: {title}, id: {state.AlertId}, owner: {owner}, is in fail state but suppressed until {state.SuppressedUntil:O}";
                Logger.LogWarning("alert: {Title}, id: {AlertId}, owner: {Owner}, is in fail state but suppressed until {SuppressedUntil:O}",
                    title,
                    state.AlertId,
                    owner,
                    state.SuppressedUntil);

                return;
            }
        }

        alert.ResultMessage = $"alert: {title}, id: {state.AlertId}, owner: {owner}, is in fail state";
        Logger.LogError("alert {Title}, id: {AlertId}, owner: {Owner}, is in fail state", title, state.AlertId, owner);

        throw new CheckException($"alert: {title}, id: {state.AlertId}, owner: {owner}, is in fail state");
    }

    private static void ValidateAlert(SeqAlert alert)
    {
        ValidateBase(alert, $"alert ({alert.Key})");
        ValidateRequired(alert.Key, "key");
    }
}