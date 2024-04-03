using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using Polly;
using System.Net;

namespace RestHealthCheck
{
    internal sealed class Job : BaseJob
    {
        public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        {
        }

        public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
        {
        }

        public async override Task ExecuteJob(IJobExecutionContext context)
        {
            var tasks = new List<Task>();
            var defaults = GetDefaults(Configuration);
            var hosts = GetHosts(Configuration);
            var endpoints = GetEndpoints(Configuration, hosts);

            using var client = new HttpClient();
            foreach (var ep in endpoints)
            {
                ValidateEndpoint(ep, defaults);
                var task = SafeInvokeEndpoint(ep, client);
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            CheckAggragateException();
        }

        // validate endpoint
        private static void ValidateEndpoint(Endpoint endpoint, Defaults defaults)
        {
            endpoint.SuccessStatusCodes ??= defaults.SuccessStatusCodes;

            if (endpoint.Timeout == null)
            {
                endpoint.Timeout = defaults.Timeout;
            }

            endpoint.RetryCount ??= defaults.RetryCount;

            if (endpoint.RetryInterval == null)
            {
                endpoint.RetryInterval = defaults.RetryInterval;
            }
        }

        private static IEnumerable<string> GetHosts(IConfiguration configuration)
        {
            var hosts = configuration.GetSection("hosts");
            if (hosts == null) { return []; }
            return hosts.Get<string[]>() ?? [];
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

        private static Defaults GetDefaults(IConfiguration configuration)
        {
            var defaults = configuration.GetRequiredSection("defaults");

            return new Defaults
            {
                SuccessStatusCodes = defaults.GetRequiredSection("success status codes").Get<int[]?>(),
                Timeout = defaults.GetValue<TimeSpan?>("Timeout"),
                RetryCount = defaults.GetValue<int?>("retry count"),
                RetryInterval = defaults.GetValue<TimeSpan?>("retry interval")
            };
        }

        private async Task SafeInvokeEndpoint(Endpoint endpoint, HttpClient client)
        {
            try
            {
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
                            var response = await client.GetAsync(endpoint.Url);
                            endpoint.SuccessStatusCodes ??= new List<int> { 200 };

                            if (endpoint.SuccessStatusCodes.Any(s => s == (int)response.StatusCode))
                            {
                                Logger.LogInformation("Health check success for endpoint name '{EndpointName}' with url '{EndpointUrl}'",
                                    endpoint.Name, endpoint.Url);
                                return;
                            }

                            throw new HealthCheckException($"Status code {response.StatusCode} ({(int)response.StatusCode}) is not in success status codes list");
                        });
            }
            catch (Exception ex)
            {
                var exception = ex is HealthCheckException ? null : ex;

                if (exception == null)
                {
#pragma warning disable S6667 // Logging in a catch clause should pass the caught exception as a parameter.
                    Logger.LogError("Health check fail for endpoint name '{EndpointName}' with url '{EndpointUrl}'. Reason: {Message}",
                      endpoint.Name, endpoint.Url, ex.Message);
#pragma warning restore S6667 // Logging in a catch clause should pass the caught exception as a parameter.
                }
                else
                {
                    Logger.LogError(exception, "Health check fail for endpoint name '{EndpointName}' with url '{EndpointUrl}'. Reason: {Message}",
                        endpoint.Name, endpoint.Url, ex.Message);
                }

                AddAggregateException(new HealthCheckException($"Health check fail for endpoint name '{endpoint.Name}' with url '{endpoint.Url}'. Reason: {ex.Message}"));
            }
        }
    }
}