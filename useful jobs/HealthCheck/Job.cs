using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using Polly;
using System.Collections.Concurrent;

namespace HealthCheck;

internal sealed class Job : BaseJob
{
    private readonly ConcurrentQueue<HealthCheckException> _exceptions = new();

    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
    }

    public async override Task ExecuteJob(IJobExecutionContext context)
    {
        var tasks = new List<Task>();
        var defaults = GetDefaults(Configuration);
        var hosts = GetHosts(Configuration);
        var endpoints = GetEndpoints(Configuration, hosts);

        if (!ValidateEndpoints(endpoints))
        {
            CheckAggragateException();
            return;
        }

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
        CheckHealthCheckExceptions();
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.AddSingleton<EndpointFailCounter>();
    }

    private void CheckHealthCheckExceptions()
    {
        if (!_exceptions.IsEmpty)
        {
            var message = $"health check failed for endpoints: {string.Join(", ", _exceptions.Select(x => x.Name).Distinct())}";
            throw new AggregateException(message, _exceptions);
        }
    }

    private static void FillDefaults(Endpoint endpoint, Defaults defaults)
    {
        // Fill Defaults
        endpoint.Name ??= string.Empty;
        endpoint.Name = endpoint.Name.Trim();
        if (string.IsNullOrWhiteSpace(endpoint.Name))
        {
            endpoint.Name = "[no name]";
        }

        endpoint.SuccessStatusCodes ??= defaults.SuccessStatusCodes;
        if (endpoint.Timeout == null) { endpoint.Timeout = defaults.Timeout; }
        endpoint.RetryCount ??= defaults.RetryCount;
        endpoint.MaximumFailsInRow ??= defaults.MaximumFailsInRow;
        if (endpoint.RetryInterval == null) { endpoint.RetryInterval = defaults.RetryInterval; }
    }

    private static IEnumerable<Endpoint> GetEndpoints(IConfiguration configuration, IEnumerable<string> hosts)
    {
        const string hostPlaceholder = "{{host}}";

        var endpoints = configuration.GetRequiredSection("endpoints");
        foreach (var item in endpoints.GetChildren())
        {
            var url = item.GetValue<string>("url") ?? string.Empty;
            if (url.StartsWith(hostPlaceholder))
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

        if (endpoint.RetryCount < 0)
        {
            throw new InvalidDataException($"'retry count' on {section} section is null or less then 0");
        }

        if (endpoint.RetryCount > 10)
        {
            throw new InvalidDataException($"'retry count' on {section} section is greater then 0");
        }

        if (endpoint.MaximumFailsInRow < 1)
        {
            throw new InvalidDataException($"'maximum fails in row' on {section} section must be greater then 0");
        }

        if (endpoint.MaximumFailsInRow > 1000)
        {
            throw new InvalidDataException($"'maximum fails in row' on {section} section must be greater less then 1000");
        }
    }

    private static void ValidateName(Endpoint endpoint)
    {
        if (endpoint.Name?.Length > 50)
        {
            throw new InvalidDataException($"length at 'name' on endpoint ({endpoint.Name}) section is greater then 50");
        }
    }

    private static void ValidateUrl(Endpoint endpoint)
    {
        if (endpoint.Url?.Length > 1000)
        {
            throw new InvalidDataException($"length at 'url' on endpoint ({endpoint.Name}) section is greater then 1000");
        }

        if (!Uri.TryCreate(endpoint.Url, UriKind.Absolute, out _))
        {
            throw new InvalidDataException($"'url' on endpoint ({endpoint.Name}) with value '{endpoint.Url}' is not valid");
        }
    }

    private Defaults GetDefaults(IConfiguration configuration)
    {
        var empty = Defaults.Empty;
        var defaults = configuration.GetSection("defaults");
        if (defaults == null)
        {
            Logger.LogWarning("No defaults section found on settings file. Set job factory defaults");
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

        return result;
    }

    private async Task InvokeEndpointInner(Endpoint endpoint, HttpClient client)
    {
        var response = await client.GetAsync(endpoint.Url);
        endpoint.SuccessStatusCodes ??= new List<int> { 200 };

        if (endpoint.SuccessStatusCodes.Any(s => s == (int)response.StatusCode))
        {
            Logger.LogInformation("Health check success for endpoint name '{EndpointName}' with url '{EndpointUrl}'",
                endpoint.Name, endpoint.Url);
            return;
        }

        throw new HealthCheckException(
            $"Status code {response.StatusCode} ({(int)response.StatusCode}) is not in success status codes list",
            endpoint.Name);
    }

    private void SafeHandleException(Endpoint endpoint, Exception ex, EndpointFailCounter counter)
    {
        try
        {
            var exception = ex is HealthCheckException ? null : ex;

            if (exception == null)
            {
                Logger.LogError("Health check fail for endpoint name '{EndpointName}' with url '{EndpointUrl}'. Reason: {Message}",
                  endpoint.Name, endpoint.Url, ex.Message);
            }
            else
            {
                Logger.LogError(exception, "Health check fail for endpoint name '{EndpointName}' with url '{EndpointUrl}'. Reason: {Message}",
                    endpoint.Name, endpoint.Url, ex.Message);
            }

            var value = counter.IncrementFailCount(endpoint);

            if (value > endpoint.MaximumFailsInRow)
            {
                Logger.LogWarning("Health check fail but maximum fails in row reached for endpoint name '{EndpointName}' with url '{EndpointUrl}'. Reason: {Message}",
                    endpoint.Name, endpoint.Url, ex.Message);
            }
            else
            {
                var hcException = new HealthCheckException(
                    $"Health check fail for endpoint name '{endpoint.Name}' with url '{endpoint.Url}'. Reason: {ex.Message}",
                    endpoint.Name);

                _exceptions.Enqueue(hcException);
            }
        }
        catch (Exception innerEx)
        {
            AddAggregateException(innerEx);
        }
    }

    private async Task SafeInvokeEndpoint(Endpoint endpoint, HttpClient client)
    {
        var counter = ServiceProvider.GetRequiredService<EndpointFailCounter>();

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
                             var exception = ex is HealthCheckException ? null : ex;
                             Logger.LogWarning(exception, "Retry for endpoint name '{EndpointName}' with url '{EndpointUrl}'. Reason: {Message}",
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

    // validate endpoint
    private bool ValidateEndpoint(Endpoint endpoint)
    {
        try
        {
            Validate(endpoint, $"endpoints ({endpoint.Name})");
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

    private bool ValidateEndpoints(IEnumerable<Endpoint> endpoints)
    {
        try
        {
            var duplicates1 = endpoints.GroupBy(x => x.Url).Where(g => g.Count() > 1).Select(y => y.Key).ToList();
            if (duplicates1.Count != 0)
            {
                throw new InvalidDataException($"duplicated endpoint urls found: {string.Join(", ", duplicates1)}");
            }
        }
        catch (Exception ex)
        {
            AddAggregateException(ex);
            return false;
        }

        return true;
    }
}