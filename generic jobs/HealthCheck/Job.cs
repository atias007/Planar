using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;

namespace HealthCheck;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
internal record HttpUtility(string Url, HttpClient Client);
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter

internal partial class Job : BaseCheckJob
{
#pragma warning disable S3251 // Implementations should be provided for "partial" methods

    partial void CustomConfigure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context);

    partial void VetoEndpoint(Endpoint endpoint);

    partial void VetoHost(Host host);

    partial void Finalayze(FinalayzeDetails<IEnumerable<Endpoint>> details);

#pragma warning restore S3251 // Implementations should be provided for "partial" methods

    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        => CustomConfigure(configurationBuilder, context);

    public async override Task ExecuteJob(IJobExecutionContext context)
    {
        Initialize(ServiceProvider);

        var defaults = GetDefaults(Configuration);
        var hosts = GetHosts(Configuration, h => VetoHost(h));
        var keys = GetKeys(context);
        var endpoints = GetEndpoints(Configuration, defaults, keys);

        if (endpoints.Exists(e => e.IsRelativeUrl))
        {
            ValidateRequired(hosts, "hosts");
        }

        ValidateDuplicateNames(endpoints, "endpoints");

        endpoints = GetEndpointsWithHost(endpoints, hosts);
        EffectedRows = 0;

        using var client = CreateHttpClient(endpoints);

        await SafeInvokeCheck(endpoints, e => InvokeEndpointInner(e, client), context.TriggerDetails);

        var details = GetFinalayzeDetails(endpoints.AsEnumerable());
        Finalayze(details);
        Finalayze();
    }

    private static HttpClient CreateHttpClient(IEnumerable<Endpoint> endpoints)
    {
        var maxTimeoutSeconts = endpoints.Max(e => e.Timeout.TotalSeconds);
        if (maxTimeoutSeconts <= 0) { maxTimeoutSeconts = 0; }
        maxTimeoutSeconts += 10;
        return new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(maxTimeoutSeconts),
        };
    }

    private static List<Endpoint> GetEndpointsWithHost(List<Endpoint> endpoints, IReadOnlyDictionary<string, HostsConfig> hosts)
    {
        var absolute = endpoints.Where(e => e.IsAbsoluteUrl);
        var relative = endpoints.Where(e => e.IsRelativeUrl);
        var result = new List<Endpoint>(absolute);
        if (relative.Any() && hosts.Count != 0)
        {
            foreach (var rel in relative)
            {
                if (!hosts.TryGetValue(rel.HostGroupName ?? string.Empty, out var hostGroup))
                {
                    throw new InvalidDataException($"endpoint '{rel.Name}' has no host group name '{rel.HostGroupName}'");
                }
                else
                {
                    var clones = hostGroup.Hosts.Select(h => new Endpoint(rel) { Host = new Uri(h) });
                    result.AddRange(clones);
                }
            }
        }

        return result;
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.RegisterSpanCheck();
    }

    private List<Endpoint> GetEndpoints(IConfiguration configuration, Defaults defaults)
    {
        var endpoints = configuration.GetRequiredSection("endpoints");
        var result = new List<Endpoint>();
        foreach (var item in endpoints.GetChildren())
        {
            var endpoint = new Endpoint(item, defaults);
            VetoEndpoint(endpoint);
            if (CheckVeto(endpoint, "endpoint")) { continue; }

            ValidateEndpoint(endpoint);
            result.Add(endpoint);
        }

        ValidateRequired(result, "endpoints");
        ValidateDuplicateNames(result, "endpoints");

        return result;
    }

    private List<Endpoint> GetEndpoints(IConfiguration configuration, Defaults defaults, IEnumerable<string>? keys)
    {
        if (keys == null || !keys.Any()) { return GetEndpoints(configuration, defaults); }

        var endpoints = configuration.GetRequiredSection("endpoints");
        var result = new List<Endpoint>();

        foreach (var item in endpoints.GetChildren())
        {
            var endpoint = new Endpoint(item, defaults);
            if (keys.Any(k => string.Equals(k, endpoint.Key, StringComparison.OrdinalIgnoreCase)))
            {
                endpoint.BindToTriggers = null;
                ValidateEndpoint(endpoint);
                result.Add(endpoint);
            }
        }

        ValidateRequired(result, "endpoints");
        ValidateDuplicateNames(result, "endpoints");

        return result;
    }

    private static void Validate(IEndpoint endpoint, string section)
    {
        ValidateGreaterThen(endpoint.Timeout, TimeSpan.FromSeconds(1), "timeout", section);
        ValidateLessThen(endpoint.Timeout, TimeSpan.FromMinutes(20), "timeout", section);
    }

    private Defaults GetDefaults(IConfiguration configuration)
    {
        var empty = Defaults.Empty;
        var section = GetDefaultSection(configuration, Logger);
        if (section == null) { return empty; }

        var result = new Defaults(section);
        Validate(result, "defaults");
        ValidateBase(result, "defaults");
        return result;
    }

    private static Uri BuildUri(Endpoint endpoint)
    {
        if (endpoint.AbsoluteUrl != null) { return endpoint.AbsoluteUrl; }

        if (endpoint.Host == null)
        {
            throw new InvalidDataException($"endpoint url '{endpoint.Url}' is relative url but not host(s) is defined");
        }

        var builder = new UriBuilder(endpoint.Host)
        {
            Path = endpoint.Url
        };

        if (endpoint.Port.HasValue)
        {
            builder.Port = endpoint.Port.Value;
        }

        return builder.Uri;
    }

    private async Task InvokeEndpointInner(Endpoint endpoint, HttpClient client)
    {
        var uri = BuildUri(endpoint);
        using var cs = new CancellationTokenSource(endpoint.Timeout);
        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(uri, cs.Token);
        }
        catch (TaskCanceledException)
        {
            endpoint.ResultMessage = $"health check fail for endpoint name '{endpoint.Name}' with url '{uri}'. timeout expire";
            throw new CheckException(endpoint.ResultMessage);
        }
        catch (Exception ex)
        {
            endpoint.ResultMessage = $"health check fail for endpoint name '{endpoint.Name}' with url '{uri}'. message: {ex.Message}";
            throw new CheckException(endpoint.ResultMessage);
        }

        endpoint.Result.HttpStatusCode = response.StatusCode;
        if (endpoint.SuccessStatusCodes.Any(s => s == (int)response.StatusCode))
        {
            endpoint.ResultMessage = $"health check success for endpoint name '{endpoint.Name}' with url '{uri}'";
            Logger.LogInformation("health check success for endpoint name '{EndpointName}' with url '{EndpointUrl}'",
                endpoint.Name, uri);

            IncreaseEffectedRows();
            return;
        }

        endpoint.ResultMessage = $"health check fail for endpoint name '{endpoint.Name}' with url '{uri}' status code {response.StatusCode} ({(int)response.StatusCode}) is not in success status codes list";
        throw new CheckException(endpoint.ResultMessage);
    }

    private static void ValidateEndpoint(Endpoint endpoint)
    {
        var section = $"endpoints ({endpoint.Name})";
        Validate(endpoint, section);
        ValidateRequired(endpoint.SuccessStatusCodes, "success status codes", section);
        ValidateBase(endpoint, section);
        ValidateMaxLength(endpoint.Name, 50, "name", section);
        ValidateRequired(endpoint.Url, "url", section);
        ValidateRequired(endpoint.Name, "name", section);
    }
}