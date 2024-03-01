using Planar.Client.Entities;
using RestSharp;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client.Api
{
    internal class ClusterApi : BaseApi, IClusterApi
    {
        public ClusterApi(RestProxy proxy) : base(proxy)
        {
        }

        public async Task<string> HealthCheckAsync(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("cluster/health-check", Method.Get);
            var result = await _proxy.InvokeAsync<string>(restRequest, cancellationToken);
            return result;
        }

        public async Task<IEnumerable<ClusterNode>> ListNodesAsync(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("cluster/nodes", Method.Get);
            var result = await _proxy.InvokeAsync<IEnumerable<ClusterNode>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<int> MaxConcurrencyAsync(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("cluster/max-concurrency", Method.Get);
            var result = await _proxy.InvokeAsync<int>(restRequest, cancellationToken);
            return result;
        }
    }
}