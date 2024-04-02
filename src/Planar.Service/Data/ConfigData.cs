using Microsoft.EntityFrameworkCore;
using Planar.Service.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Data;

public class ConfigData(PlanarContext context) : BaseDataLayer(context)
{
    public async Task<GlobalConfig?> GetGlobalConfig(string key)
    {
        var result = await _context.GlobalConfigs.AsNoTracking().FirstOrDefaultAsync(c => c.Key == key);
        return result;
    }

    public async Task<bool> IsGlobalConfigExists(string key)
    {
        var result = await _context.GlobalConfigs.AnyAsync(p => p.Key == key);
        return result;
    }

    public async Task<IEnumerable<GlobalConfig>> GetAllGlobalConfig(CancellationToken stoppingToken = default)
    {
        var result = await _context.GlobalConfigs.AsNoTracking().OrderBy(p => p.Key).ToListAsync(stoppingToken);
        return result;
    }

    public async Task AddGlobalConfig(GlobalConfig config)
    {
        _context.GlobalConfigs.Add(config);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateGlobalConfig(GlobalConfig config)
    {
        _context.GlobalConfigs.Update(config);
        await _context.SaveChangesAsync();
    }

    public async Task<int> RemoveGlobalConfig(string key)
    {
        var count = await _context.GlobalConfigs
            .Where(g => g.Key == key)
            .ExecuteDeleteAsync();
        return count;
    }
}