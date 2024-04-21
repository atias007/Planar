using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using Polly;

namespace HealthCheck;

internal sealed class Job : BaseCheckJob
{
    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
    }

    public async override Task ExecuteJob(IJobExecutionContext context)
    {
        var tasks = new List<Task>();
        var defaults = GetDefaults(Configuration);
        var hosts = GetHosts(Configuration);
        var endpoints = GetEndpoints(Configuration, hosts);
        ValidateEndpoints(endpoints);
        CheckAggragateException();

        using var client = new HttpClient();
        foreach (var ep in endpoints)
        {
            FillDefaults(ep, defaults);
            if (!ValidateEndpoint(ep)) { continue; }
            var task = SafeInvokeEndpoint(ep, client);
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        CheckAggragateException();
        HandleCheckExceptions();
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.AddSingleton<CheckFailCounter>();
    }

    private static void FillDefaults(Endpoint endpoint, Defaults defaults)
    {
        SetDefaultName(endpoint, () => endpoint.Name);
        SetDefault(endpoint, () => defaults.SuccessStatusCodes);
        SetDefault(endpoint, () => defaults.Timeout);
        FillBase(endpoint, defaults);
    }

    private static IEnumerable<Endpoint> GetEndpoints(IConfiguration configuration, IEnumerable<string> hosts)
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
                    yield return endpoint;
                }
            }
            else
            {
                var endpoint = new Endpoint(item, url);
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

        if ((endpoint.RetryInterval?.TotalSeconds ?? 0) < 1)
        {
            throw new InvalidDataException($"'retry interval' on {section} section is null or less then 1 second");
        }

        if ((endpoint.RetryInterval?.TotalMinutes ?? 0) > 1)
        {
            throw new InvalidDataException($"'retry interval' on {section} section is greater then 1 minutes");
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
        if (string.IsNullOrWhiteSpace(endpoint.Url))
        {
            throw new InvalidDataException($"'url' on endpoint name '{endpoint.Name}' is null or empty");
        }

        if (endpoint.Url.Length > 1000)
        {
            throw new InvalidDataException($"'url' on endpoint name '{endpoint.Name}' must be less then 1000");
        }

        if (!Uri.TryCreate(endpoint.Url, UriKind.Absolute, out _))
        {
            throw new InvalidDataException($"'url' on endpoint name '{endpoint.Name}' with value '{endpoint.Url}' is not valid uri");
        }
    }

    private Defaults GetDefaults(IConfiguration configuration)
    {
        var empty = Defaults.Empty;
        var defaults = configuration.GetSection("defaults");
        if (defaults == null)
        {
            Logger.LogWarning("no defaults section found on settings file. set job factory defaults");
            return empty;
        }

        var result = new Defaults
        {
            SuccessStatusCodes = defaults.GetRequiredSection("success status codes").Get<int[]?>(),
            Timeout = defaults.GetValue<TimeSpan?>("timeout") ?? empty.Timeout,
            RetryCount = defaults.GetValue<int?>("retry count") ?? empty.RetryCount,
            RetryInterval = defaults.GetValue<TimeSpan?>("retry interval") ?? empty.RetryInterval,
            MaximumFailsInRow = defaults.GetValue<int?>("maximum fails in row") ?? empty.MaximumFailsInRow,
        };

        Validate(result, "defaults");
        ValidateBase(result, "defaults");

        return result;
    }

    private async Task InvokeEndpointInner(Endpoint endpoint, HttpClient client)
    {
        var response = await client.GetAsync(endpoint.Url);
        endpoint.SuccessStatusCodes ??= new List<int> { 200 };

        if (endpoint.SuccessStatusCodes.Any(s => s == (int)response.StatusCode))
        {
            Logger.LogInformation("health check success for endpoint name '{EndpointName}' with url '{EndpointUrl}'",
                endpoint.Name, endpoint.Url);
            return;
        }

        throw new CheckException($"Status code {response.StatusCode} ({(int)response.StatusCode}) is not in success status codes list");
    }

    private void SafeHandleException(Endpoint endpoint, Exception ex, CheckFailCounter counter)
    {
        try
        {
            var exception = ex is CheckException ? null : ex;

            if (exception == null)
            {
                Logger.LogError("health check fail for endpoint name '{EndpointName}' with url '{EndpointUrl}'. reason: {Message}",
                  endpoint.Name, endpoint.Url, ex.Message);
            }
            else
            {
                Logger.LogError(exception, "health check fail for endpoint name '{EndpointName}' with url '{EndpointUrl}'. reason: {Message}",
                    endpoint.Name, endpoint.Url, ex.Message);
            }

            var value = counter.IncrementFailCount(endpoint);

            if (value > endpoint.MaximumFailsInRow)
            {
                Logger.LogWarning("health check fail for endpoint name '{EndpointName}' with url '{EndpointUrl}' but maximum fails in row reached. reason: {Message}",
                    endpoint.Name, endpoint.Url, ex.Message);
            }
            else
            {
                var hcException = new CheckException($"health check fail for endpoint name '{endpoint.Name}' with url '{endpoint.Url}. reason: {ex.Message}");

                AddCheckException(hcException);
            }
        }
        catch (Exception innerEx)
        {
            AddAggregateException(innerEx);
        }
    }

    private async Task SafeInvokeEndpoint(Endpoint endpoint, HttpClient client)
    {
        var counter = ServiceProvider.GetRequiredService<CheckFailCounter>();

        try
        {
            if (endpoint.RetryCount == 0)
            {
                await InvokeEndpointInner(endpoint, client);
                return;
            }

            await Policy.Handle<Exception>()
                    .WaitAndRetryAsync(
                        retryCount: endpoint.RetryCount.GetValueOrDefault(),
                        sleepDurationProvider: _ => endpoint.RetryInterval.GetValueOrDefault(),
                         onRetry: (ex, _) =>
                         {
                             var exception = ex is CheckException ? null : ex;
                             Logger.LogWarning(exception, "retry for endpoint name '{EndpointName}' with url '{EndpointUrl}'. Reason: {Message}",
                                                                     endpoint.Name, endpoint.Url, ex.Message);
                         })
                    .ExecuteAsync(async () =>
                    {
                        await InvokeEndpointInner(endpoint, client);
                    });

            counter.ResetFailCount(endpoint);
        }
        catch (Exception ex)
        {
            SafeHandleException(endpoint, ex, counter);
        }
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

    private void ValidateEndpoints(IEnumerable<Endpoint> endpoints)
    {
        try
        {
            if (endpoints == null || !endpoints.Any())
            {
                throw new InvalidDataException("endpoints section is null or empty");
            }

            var duplicates1 = endpoints.GroupBy(x => x.Url).Where(g => g.Count() > 1).Select(y => y.Key).ToList();
            if (duplicates1.Count != 0)
            {
                throw new InvalidDataException($"duplicated endpoint urls found: {string.Join(", ", duplicates1)}");
            }
        }
        catch (Exception ex)
        {
            AddAggregateException(ex);
        }
    }
}