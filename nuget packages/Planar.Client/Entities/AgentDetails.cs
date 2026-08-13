using System;

namespace Planar.Client.Entities
{
    public class AgentDetails
    {
#if NETSTANDARD2_0
        public string ClientId { get; set; }
        public string IpAddress { get; set; }
#else
        public string ClientId { get; set; } = null!;
        public string? IpAddress { get; set; } = null!;
#endif
        public DateTime LastSeen { get; set; }
        public TimeSpan NotSeenSpan => DateTime.UtcNow.Subtract(LastSeen.ToUniversalTime());
        public int Status { get; set; }
        public string StatusTitle { get; set; } = "Unknown";
    }
}