using Planar.CLI.Proxy;
using RestSharp;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.Actions;

internal static class SpecialActions
{
    public static async Task<IEnumerable<string>?> GetJobIds(CancellationToken cancellationToken = default)
    {
        var restRequest = new RestRequest("job/ids", Method.Get);
        var response = await RestProxy.InvokeInner<IEnumerable<string>>(restRequest, cancellationToken);
        if (!response.IsSuccessful || response.Data == null) { return null; }
        return response.Data;
    }

    public static async Task<IEnumerable<string>?> GetTriggerIds(CancellationToken cancellationToken = default)
    {
        var restRequest = new RestRequest("trigger/ids", Method.Get);
        var response = await RestProxy.InvokeInner<IEnumerable<string>>(restRequest, cancellationToken);
        if (!response.IsSuccessful || response.Data == null) { return null; }
        return response.Data;
    }

    public static async Task<bool> IsJobId(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) { return false; }
        var restRequest = new RestRequest("job/{id}", Method.Get)
            .AddParameter("id", id, ParameterType.UrlSegment);

        var result = await RestProxy.InvokeInner(restRequest, cancellationToken);
        return result.IsSuccessStatusCode;
    }

    public static async Task<bool> IsTriggerId(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) { return false; }
        var restRequest = new RestRequest("trigger/{triggerId}", Method.Get)
            .AddParameter("triggerId", id, ParameterType.UrlSegment);

        var result = await RestProxy.InvokeInner(restRequest, cancellationToken);
        return result.IsSuccessStatusCode;
    }
}