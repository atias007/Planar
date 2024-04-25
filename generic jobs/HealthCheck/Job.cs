using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;

namespace HealthCheck;

internal sealed class Job : BaseCheckJob
{
    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
    }

    public async override Task ExecuteJob(IJobExecutionContext context)
    {
        Initialize(ServiceProvider);

        var defaults = GetDefaults(Configuration);
        var hosts = GetHosts(Configuration);
        var endpoints = GetEndpoints(Configuration, defaults);

        if (!hosts.Any() && endpoints.Exists(e => e.AbsoluteUrl == null))
        {
            throw new InvalidDataException("no hosts defined and at least one endpoint is not absolute url");
        }

        using var client = new HttpClient();
        var tasks = SafeInvokeCheck(endpoints, ep => InvokeEndpointsInner(ep, hosts, client));
        await Task.WhenAll(tasks);

        CheckAggragateException();
        HandleCheckExceptions();
    }

    private static void ValidateEndpoints(IEnumerable<Endpoint> endpoints)
    {
        ValidateRequired(endpoints, "endpoints");
        ValidateDuplicateNames(endpoints, "endpoints");
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.RegisterBaseCheck();
    }

    private static void FillDefaults(Endpoint endpoint, Defaults defaults)
    {
        SetDefaultName(endpoint, () => endpoint.Name);
        endpoint.SuccessStatusCodes ??= defaults.SuccessStatusCodes;
        endpoint.Timeout ??= defaults.Timeout;
        FillBase(endpoint, defaults);
    }

    private static List<Endpoint> GetEndpoints(IConfiguration configuration, Defaults defaults)
    {
        var endpoints = configuration.GetRequiredSection("endpoints");
        var result = new List<Endpoint>();
        foreach (var item in endpoints.GetChildren())
        {
            var endpoint = new Endpoint(item);
            FillDefaults(endpoint, defaults);
            ValidateEndpoint(endpoint);
            result.Add(endpoint);
        }

        ValidateEndpoints(result);
        return result;
    }

    private static IEnumerable<Uri> GetHosts(IConfiguration configuration)
    {
        var hosts = configuration.GetSection("hosts");
        if (hosts == null) { return []; }
        var result = hosts.Get<string[]>() ?? [];
        result.ToList().ForEach(h => ValidateUri(h, "host", "hosts"));
        return result.Distinct().Select(r => new Uri(r));
    }

    private static void Validate(IEndpoint endpoint, string section)
    {
        if (!endpoint.SuccessStatusCodes?.Any() ?? true)
        {
            throw new InvalidDataException($"'success status codes' on {section} section is null or empty");
        }

        if ((endpoint.Timeout?.TotalSeconds ?? 0) < 1)
        {
            throw new InvalidDataException($"'timeout' on {section} section is null or less then 1 second");
        }

        if ((endpoint.Timeout?.TotalMinutes ?? 0) > 20)
        {
            throw new InvalidDataException($"'timeout' on {section} section is greater then 20 minutes");
        }
    }

    private Defaults GetDefaults(IConfiguration configuration)
    {
        var empty = Defaults.Empty;
        var section = GetDefaultSection(configuration, Logger);
        if (section == null) { return empty; }

        var result = new Defaults(section)
        {
            SuccessStatusCodes = section.GetSection("success status codes").Get<int[]?>() ?? empty.SuccessStatusCodes,
            Timeout = section.GetValue<TimeSpan?>("timeout") ?? empty.Timeout
        };

        Validate(result, "defaults");
        ValidateBase(result, "defaults");
        return result;
    }

    private static Uri BuildUri(Uri? baseUri, Endpoint endpoint)
    {
        if (endpoint.AbsoluteUrl != null) { return endpoint.AbsoluteUrl; }

        if (baseUri == null)
        {
            throw new InvalidDataException($"endpoint url '{endpoint.Url}' is relative url but not host(s) is defined");
        }

        var builder = new UriBuilder(baseUri)
        {
            Path = endpoint.Url
        };

        if (endpoint.Port.HasValue)
        {
            builder.Port = endpoint.Port.Value;
        }

        return builder.Uri;
    }

    private async Task InvokeEndpointsInner(Endpoint endpoint, IEnumerable<Uri> hosts, HttpClient client)
    {
        if (endpoint.AbsoluteUrl == null)
        {
            await Parallel.ForEachAsync(hosts, (host, ct) => InvokeEndpointInner(endpoint, host, client));
        }
        else
        {
            await InvokeEndpointInner(endpoint, null, client);
        }
    }

    private async ValueTask InvokeEndpointInner(Endpoint endpoint, Uri? host, HttpClient client)
    {
        if (!endpoint.Active)
        {
            Logger.LogInformation("skipping inactive endpoint '{Name}'", endpoint.Name);
            return;
        }

        var uri = BuildUri(host, endpoint);

        HttpResponseMessage response;
        try
        {
            using var source = new CancellationTokenSource(endpoint.Timeout.GetValueOrDefault());
            response = await client.GetAsync(uri, source.Token);
        }
        catch (TaskCanceledException)
        {
            throw new CheckException($"health check fail for endpoint name '{endpoint.Name}' with url '{endpoint.Url}'. timeout expire");
        }

        endpoint.SuccessStatusCodes ??= new List<int> { 200 };

        if (endpoint.SuccessStatusCodes.Any(s => s == (int)response.StatusCode))
        {
            Logger.LogInformation("health check success for endpoint name '{EndpointName}' with url '{EndpointUrl}'",
                endpoint.Name, uri);
            return;
        }

        throw new CheckException($"health check fail for endpoint name '{endpoint.Name}' with url '{endpoint.Url}' status code {response.StatusCode} ({(int)response.StatusCode}) is not in success status codes list");
    }

    private static void ValidateEndpoint(Endpoint endpoint)
    {
        var section = $"endpoints ({endpoint.Name})";
        Validate(endpoint, section);
        ValidateBase(endpoint, section);
        ValidateMaxLength(endpoint.Name, 50, "name", section);
        ValidateRequired(endpoint.Url, "url", section);
        ValidateRequired(endpoint.Name, "name", section);
    }
}