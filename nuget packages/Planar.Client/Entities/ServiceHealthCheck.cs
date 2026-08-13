using System.Text.Json.Serialization;

namespace Planar.Client.Entities
{
    public class ServiceHealthCheck
    {
#if NETSTANDARD2_0

        [JsonPropertyName("scheduler")]
        public HealthCheckResponse Scheduler { get; set; }

        [JsonPropertyName("database")]
        public HealthCheckResponse Database { get; set; }

        [JsonPropertyName("cluster")]
        public HealthCheckResponse Cluster { get; set; }

#else
        [JsonPropertyName("scheduler")]
        public HealthCheckResponse Scheduler { get; set; } = null!;

        [JsonPropertyName("database")]
        public HealthCheckResponse Database { get; set; } = null!;

        [JsonPropertyName("cluster")]
        public HealthCheckResponse Cluster { get; set; } = null!;
#endif
    }

    public class HealthCheckResponse
    {
        [JsonPropertyName("notRelevant")]
        public bool NotRelevant { get; set; }

        [JsonPropertyName("isHealthy")]
        public bool IsHealthy { get; set; }

#if NETSTANDARD2_0

        [JsonPropertyName("title")]
        public string Title { get; set; }

#else
        [JsonPropertyName("title")]
        public string Title { get; set; } = null!;
#endif
    }
}