using System;

namespace Planar.API.Common.Entities;

public class AgentModel
{
    public required string ClientId { get; set; }
    public required string IpAddress { get; set; }
    public DateTime LastSeen { get; set; }
    public TimeSpan NotSeenSpan => DateTime.UtcNow.Subtract(LastSeen.ToUniversalTime());
    public int Status { get; set; }
    public string StatusTitle { get; set; } = "Unknown";
}