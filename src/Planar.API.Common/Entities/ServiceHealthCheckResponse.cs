using System.Text.Json.Serialization;

namespace Planar.API.Common.Entities;

public class ServiceHealthCheckResponse
{
    [JsonPropertyName("scheduler")]
    public required HealthCheckResponse Scheduler { get; set; }

    [JsonPropertyName("database")]
    public required HealthCheckResponse Database { get; set; }

    [JsonPropertyName("cluster")]
    public required HealthCheckResponse Cluster { get; set; }
}

public class HealthCheckResponse
{
    [JsonPropertyName("isHealthy")]
    public bool IsHealthy { get; set; }

    [JsonPropertyName("title")]
    public required string Title { get; set; }
}