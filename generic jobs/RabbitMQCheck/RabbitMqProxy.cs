using Common;
using Microsoft.Extensions.Logging;
using RestSharp;
using RestSharp.Authenticators;
using System.Collections.Concurrent;

namespace RabbitMQCheck;

internal class RabbitMqProxy
{
    private static readonly ConcurrentDictionary<string, RabbitMqProxy> _proxies = new();
    private static readonly object _lock = new();
    private readonly RestClient _restClient;
    private readonly ILogger logger;

    public static RabbitMqProxy GetProxy(string uri, Server server, ILogger logger)
    {
        lock (_lock)
        {
            return _proxies.GetOrAdd(uri, key => new RabbitMqProxy(key, server, logger));
        }
    }

    private RabbitMqProxy(string uri, Server server, ILogger logger)
    {
        var options = new RestClientOptions
        {
            BaseUrl = new Uri(uri),
            Timeout = TimeSpan.FromSeconds(10),
            Authenticator = new HttpBasicAuthenticator(server.Username, server.Password)
        };

        _restClient = new RestClient(options);
        this.logger = logger;
    }

    public async Task ClusterAlarm()
    {
        await Alarm("cluster alarms", "alarms");
    }

    public async Task LocalAlarm()
    {
        await Alarm("local alarms", "local-alarms");
    }

    public async Task NodeMirrorSync()
    {
        await Alarm("node mirror sync", "node-is-mirror-sync-critical");
    }

    public async Task NodeQuorumCritical()
    {
        await Alarm("node quorum critical", "node-is-quorum-critical");
    }

    public async Task<IEnumerable<NodeDetails>> GetNodeDetails()
    {
        const string resource = "api/nodes";
        var request = new RestRequest(resource, Method.Get);
        var response = await _restClient.ExecuteAsync<IEnumerable<NodeDetails>>(request);
        if (!response.IsSuccessful)
        {
            throw new CheckException($"node check on host {_restClient.Options.BaseUrl} failed. status code {response.StatusCode}. content: {response.Content}", response.ErrorException);
        }

        return response.Data ?? [];
    }

    public async Task<IEnumerable<QueueDetails>> GetQueueDetails()
    {
        const string resource = "api/queues";
        var request = new RestRequest(resource, Method.Get);
        var response = await _restClient.ExecuteAsync<IEnumerable<QueueDetails>>(request);
        if (!response.IsSuccessful)
        {
            throw new CheckException($"queue check on host {_restClient.Options.BaseUrl} failed. status code {response.StatusCode}. content: {response.Content}", response.ErrorException);
        }

        return response.Data ?? [];
    }

    private async Task Alarm(string name, string resource)
    {
        const string baseResource = "api/health/checks";
        var request = new RestRequest($"{baseResource}/{resource}", Method.Get);
        var response = await _restClient.ExecuteAsync(request);
        if (!response.IsSuccessful)
        {
            throw new CheckException($"{name} check on host {_restClient.Options.BaseUrl} failed. status code {response.StatusCode}. content: {response.Content}", response.ErrorException);
        }

        logger.LogInformation("health-check '{Name}' on host {Host} succeeded", name, _restClient.BuildUri(request));
    }
}