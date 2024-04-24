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
        var endpoints = GetEndpoints(Configuration, hosts, defaults);
        ValidateRequired(endpoints, "folders");
        ValidateDuplicateKeys(endpoints, "folders");
        ValidateDuplicateNames(endpoints, "folders");

        using var client = new HttpClient();
        var tasks = SafeInvokeCheck(endpoints, ep => InvokeEndpointInner(ep, client));
        await Task.WhenAll(tasks);

        CheckAggragateException();
        HandleCheckExceptions();
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

    private static IEnumerable<Endpoint> GetEndpoints(IConfiguration configuration, IEnumerable<string> hosts, Defaults defaults)
    {
        const string hostPlaceholder = "{{host}}";

        var endpoints = configuration.GetRequiredSection("endpoints");
        foreach (var item in endpoints.GetChildren())
        {
            var url = item.GetValue<string>("url") ?? string.Empty;
            if (url.Contains(hostPlaceholder))
            {
                foreach (var host in hosts)
                {
                    var url2 = url.Replace(hostPlaceholder, host);
                    var endpoint = new Endpoint(item, url2);
                    FillDefaults(endpoint, defaults);
                    yield return endpoint;
                }
            }
            else
            {
                var endpoint = new Endpoint(item, url);
                FillDefaults(endpoint, defaults);
                yield return endpoint;
            }
        }
    }

    private static IEnumerable<string> GetHosts(IConfiguration configuration)
    {
        var hosts = configuration.GetSection("hosts");
        if (hosts == null) { return []; }
        var result = hosts.Get<string[]>() ?? [];
        return result.Distinct();
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

    private static void ValidateName(Endpoint endpoint)
    {
        if (endpoint.Name?.Length > 50)
        {
            throw new InvalidDataException($"'name' on endpoint name '{endpoint.Name}' must be less then 50");
        }
    }

    private static void ValidateUrl(Endpoint endpoint)
    {
        ValidateRequired(endpoint.Url, "url", endpoint.Name);
        ValidateMaxLength(endpoint.Url, 1000, "url", endpoint.Name);
        ValidateUri(endpoint.Url, "url", endpoint.Name);
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

    private async Task InvokeEndpointInner(Endpoint endpoint, HttpClient client)
    {
        if (!endpoint.Active)
        {
            Logger.LogInformation("Skipping inactive endpoint '{Name}'", endpoint.Name);
            return;
        }

        if (!ValidateEndpoint(endpoint)) { return; }

        HttpResponseMessage response;
        try
        {
            using var source = new CancellationTokenSource(endpoint.Timeout.GetValueOrDefault());
            response = await client.GetAsync(endpoint.Url, source.Token);
        }
        catch (TaskCanceledException)
        {
            throw new CheckException($"health check fail for endpoint name '{endpoint.Name}' with url '{endpoint.Url}'. timeout expire");
        }

        endpoint.SuccessStatusCodes ??= new List<int> { 200 };

        if (endpoint.SuccessStatusCodes.Any(s => s == (int)response.StatusCode))
        {
            Logger.LogInformation("health check success for endpoint name '{EndpointName}' with url '{EndpointUrl}'",
                endpoint.Name, endpoint.Url);
            return;
        }

        throw new CheckException($"health check fail for endpoint name '{endpoint.Name}' with url '{endpoint.Url}' status code {response.StatusCode} ({(int)response.StatusCode}) is not in success status codes list");
    }

    private bool ValidateEndpoint(Endpoint endpoint)
    {
        try
        {
            Validate(endpoint, $"endpoints ({endpoint.Name})");
            ValidateBase(endpoint, $"endpoints ({endpoint.Name})");
            ValidateName(endpoint);
            ValidateUrl(endpoint);
        }
        catch (Exception ex)
        {
            AddAggregateException(ex);
            return false;
        }

        return true;
    }
}