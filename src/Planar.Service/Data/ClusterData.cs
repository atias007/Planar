using Microsoft.EntityFrameworkCore;
using Planar.Service.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.Service.Data
{
    public class ClusterData : BaseDataLayer
    {
        public ClusterData(PlanarContext context) : base(context)
        {
        }

        public async Task<ClusterNode> GetClusterNode(ClusterNode item)
        {
            return await _context.ClusterNodes.FirstOrDefaultAsync(c => c.Server == item.Server && c.Port == item.Port);
        }

        public async Task<List<ClusterNode>> GetClusterNodes()
        {
            return await _context.ClusterNodes.ToListAsync();
        }

        public async Task AddClusterNode(ClusterNode item)
        {
            await _context.ClusterNodes.AddAsync(item);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveClusterNode(ClusterNode item)
        {
            _context.ClusterNodes.Remove(item);
            await _context.SaveChangesAsync();
        }
    }
}