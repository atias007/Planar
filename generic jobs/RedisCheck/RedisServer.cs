using System.Text.Json.Serialization;

namespace Redis;

internal class RedisServer
{
    public int Database { get; set; }
    public bool Ssl { get; set; }
    public string? User { get; set; }
    public string? Password { get; set; }
    public List<string> Endpoints { get; set; } = [];

    [JsonPropertyName("service name")]
    [Newtonsoft.Json.JsonProperty("service name")]
    public string? ServiceName { get; set; }

    [JsonIgnore]
    public bool IsEmpty => Endpoints.Count == 0;
}