using Microsoft.EntityFrameworkCore;
using Planar.API.Common.Entities;
using Planar.Common.PeriodicalBatch;
using Planar.Service.Model;
using RepoDb;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.Data;

public interface IServiceData : IBaseDataLayer
{
    Task AddSecurityAudit(SecurityAudit audit);

    IQueryable<SecurityAudit> GetSecurityAudits(SecurityAuditsFilter request);

    Task<IEnumerable<Agent>> GetAgents();

    void AddAgent(Agent agent);

    void RemoveAgent(Agent agent);

    Task HealthCheck();
}

public class ServiceDataSqlite(PlanarContext context) : ServiceData(context), IServiceData
{
}

public class ServiceDataSqlServer(PlanarContext context) : ServiceData(context), IServiceData
{
}

public class ServiceData(PlanarContext context) : BaseDataLayer(context)
{
    public async Task<IEnumerable<Agent>> GetAgents()
    {
        return await _context.Agents.ToListAsync();
    }

    public void AddAgent(Agent agent)
    {
        _context.Agents.Add(agent);
    }

    public void RemoveAgent(Agent agent)
    {
        _context.Agents.Remove(agent);
    }

    public async Task HealthCheck()
    {
        const string query = "SELECT 1";
        await _context.Database.ExecuteSqlRawAsync(query);
    }

    public async Task AddSecurityAudit(SecurityAudit audit)
    {
        _context.Add(audit);
        await _context.SaveChangesAsync();
    }

    public IQueryable<SecurityAudit> GetSecurityAudits(SecurityAuditsFilter request)
    {
        var query = _context.SecurityAudits.AsNoTracking();

        if (request.FromDate.HasValue)
        {
            query = query.Where(l => l.DateCreated >= request.FromDate);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(l => l.DateCreated < request.ToDate);
        }

        if (request.Ascending)
        {
            query = query.OrderBy(l => l.DateCreated);
        }
        else
        {
            query = query.OrderByDescending(l => l.DateCreated);
        }

        return query;
    }
}