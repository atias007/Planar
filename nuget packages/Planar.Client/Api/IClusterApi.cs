using Planar.Client.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client.Api
{
    public interface IClusterApi
    {
        Task<IEnumerable<ClusterNode>> ListNodesAsync(CancellationToken cancellationToken = default);

        Task<string> HealthCheckAsync(CancellationToken cancellationToken = default);

        Task<int> MaxConcurrencyAsync(CancellationToken cancellationToken = default);
    }
}