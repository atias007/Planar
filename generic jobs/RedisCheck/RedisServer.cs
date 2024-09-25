using Common;

namespace Redis;

internal class RedisServer
{
    internal int Database { get; set; }
    internal bool Ssl { get; set; }
    internal string? User { get; set; }
    internal string? Password { get; set; }
    internal IEnumerable<string> Endpoints { get; private set; } = [];
    internal bool IsEmpty => !Endpoints.Any();
}