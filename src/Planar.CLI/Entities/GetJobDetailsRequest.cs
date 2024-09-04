using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class GetJobDetailsRequest : CliJobKey
    {
        [ActionProperty("cb", "circuit-breaker")]
        public bool CircuitBreaker { get; set; }
    }
}