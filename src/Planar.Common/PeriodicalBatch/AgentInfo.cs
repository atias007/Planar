using System;

namespace Planar.Common.PeriodicalBatch;

public class AgentInfo
{
    public required string ClientId { get; set; }
    public required string IpAddress { get; set; }
    public DateTime LastSeen { get; set; }
}