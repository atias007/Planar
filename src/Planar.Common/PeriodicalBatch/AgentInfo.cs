using System;

namespace Planar.Common.PeriodicalBatch;

public class AgentInfo
{
    public string ClientId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime LastSeen { get; set; }
}