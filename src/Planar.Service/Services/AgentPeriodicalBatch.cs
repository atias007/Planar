using Microsoft.Extensions.DependencyInjection;
using Planar.Common.PeriodicalBatch;
using Planar.Service.Data;
using Planar.Service.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.Services;

public class AgentPeriodicalBatch(IServiceProvider serviceProvider) :
    PeriodicalBatch<AgentInfo>(serviceProvider)
{
    protected override async Task HandleBatch(IEnumerable<AgentInfo> items)
    {
        const string template = "MachineName:";
        var filterItems = items.Where(x => x.ClientId.StartsWith(template, StringComparison.OrdinalIgnoreCase));
        var groupItems = filterItems
            .GroupBy(x => x.ClientId)
            .Select(x => new Agent
            {
                ClientId = x.Key[template.Length..],
                IpAddress = x.LastOrDefault(r => !string.IsNullOrWhiteSpace(r.IpAddress))?.IpAddress ?? string.Empty,
                LastSeen = x.Max(y => y.LastSeen)
            });

        try
        {
            await SaveChanges(groupItems);
        }
        catch
        {
            await Task.Delay(1_000);
            await SaveChanges(groupItems);
        }
    }

    private async Task SaveChanges(IEnumerable<Agent> groupItems)
    {
        var dal = ServiceProvider.GetRequiredService<IServiceData>();
        var agents = await dal.GetAgents();
        foreach (var item in groupItems)
        {
            var agent = agents.FirstOrDefault(x => x.ClientId.Equals(item.ClientId, StringComparison.OrdinalIgnoreCase));
            if (agent == null && string.IsNullOrWhiteSpace(item.IpAddress)) { continue; }
            if (agent == null) { dal.AddAgent(item); continue; }
            if (!string.IsNullOrWhiteSpace(item.IpAddress)) { agent.IpAddress = item.IpAddress; }
            agent.LastSeen = item.LastSeen;
        }

        await dal.SaveChangesAsync();
    }

    protected override Task HealthCheck()
    {
        return Task.CompletedTask;
    }
}