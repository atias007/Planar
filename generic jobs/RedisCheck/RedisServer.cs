using Common;

namespace Redis;

internal class RedisServer
{
    public int Database { get; set; }
    public bool Ssl { get; set; }
    public string? User { get; set; }
    public string? Password { get; set; }
    public List<string> Endpoints { get; set; } = [];
    public string? ServiceName { get; set; }
    public bool IsEmpty => Endpoints.Count == 0;
}