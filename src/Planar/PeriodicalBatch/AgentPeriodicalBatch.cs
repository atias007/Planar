using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.PeriodicalBatch;

internal record AgentInfo(string ClientId, string IpAddress, DateTimeOffset LastSeen);

internal class AgentPeriodicalBatch(IServiceProvider serviceProvider) : PeriodicalBatch<AgentInfo>(serviceProvider)
{
    protected override Task HandleBatch(IEnumerable<AgentInfo> items)
    {
        return Task.CompletedTask;
    }

    protected override Task HealthCheck()
    {
        return Task.CompletedTask;
    }
}