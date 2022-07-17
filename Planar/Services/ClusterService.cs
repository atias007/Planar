using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Planar
{
    public class ClusterService : PlanarCluster.PlanarClusterBase
    {
        private readonly ILogger<ClusterService> _logger;

        public ClusterService(ILogger<ClusterService> logger)
        {
            _logger = logger;
        }

        public override Task<Empty> HealthCheck(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new Empty());
        }
    }
}