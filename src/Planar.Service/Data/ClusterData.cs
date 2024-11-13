using Microsoft.EntityFrameworkCore;
using Planar.Service.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.Data;

public interface IClusterData : IBaseDataLayer
{
    Task AddClusterNode(ClusterNode item);

    Task<ClusterNode?> GetClusterNode(ClusterNode item);

    Task<List<ClusterNode>> GetClusterNodes();

    Task RemoveClusterNode(ClusterNode item);

    Task UpdateClusterNode(ClusterNode item);
}

public class ClusterDataSqlServer(PlanarContext context) : ClusterData(context), IClusterData
{
}

public class ClusterDataSqlite(PlanarContext context) : ClusterData(context), IClusterData
{
}

public class ClusterData(PlanarContext context) : BaseDataLayer(context)
{
    public async Task<ClusterNode?> GetClusterNode(ClusterNode item)
    {
        return await _context.ClusterNodes.FirstOrDefaultAsync(c => c.Server == item.Server && c.Port == item.Port);
    }

    public async Task<List<ClusterNode>> GetClusterNodes()
    {
        return await _context.ClusterNodes.ToListAsync();
    }

    public async Task AddClusterNode(ClusterNode item)
    {
        _context.ClusterNodes.Add(item);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateClusterNode(ClusterNode item)
    {
        await _context.ClusterNodes
            .Where(n => n.Server == item.Server && n.Port == item.Port)
            .ExecuteUpdateAsync(u => u
                .SetProperty(p => p.InstanceId, item.InstanceId)
                .SetProperty(p => p.ClusterPort, item.ClusterPort)
                .SetProperty(p => p.JoinDate, item.JoinDate)
                .SetProperty(p => p.HealthCheckDate, item.HealthCheckDate)
                .SetProperty(p => p.MaxConcurrency, item.MaxConcurrency));
    }

    public async Task RemoveClusterNode(ClusterNode item)
    {
        await _context.ClusterNodes
            .Where(n => n.Server == item.Server && n.Port == item.Port)
            .ExecuteDeleteAsync();
    }
}