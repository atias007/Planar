using Planar.Client.Entities;
using System.Collections.Generic;
using System.Net.Http;
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
            var restRequest = new RestRequest("cluster/health-check", HttpMethod.Get);
            var result = await _proxy.InvokeAsync<string>(restRequest, cancellationToken);
            return result;
        }

        public async Task<IEnumerable<ClusterNode>> ListNodesAsync(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("cluster/nodes", HttpMethod.Get);
            var result = await _proxy.InvokeAsync<IEnumerable<ClusterNode>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<int> MaxConcurrencyAsync(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("cluster/max-concurrency", HttpMethod.Get);
            var result = await _proxy.InvokeScalarAsync<int>(restRequest, cancellationToken);
            return result;
        }
    }
}