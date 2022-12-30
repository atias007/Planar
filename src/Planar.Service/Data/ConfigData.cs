using Microsoft.EntityFrameworkCore;
using Planar.Service.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Data
{
    public class ConfigData : BaseDataLayer
    {
        public ConfigData(PlanarContext context) : base(context)
        {
        }

        public async Task<GlobalConfig> GetGlobalConfig(string key)
        {
            var result = await _context.GlobalConfigs.FindAsync(key);
            return result;
        }

        public async Task<bool> IsGlobalConfigExists(string key)
        {
            var result = await _context.GlobalConfigs.AnyAsync(p => p.Key == key);
            return result;
        }

        public async Task<IEnumerable<GlobalConfig>> GetAllGlobalConfig(CancellationToken stoppingToken = default)
        {
            var result = await _context.GlobalConfigs.OrderBy(p => p.Key).ToListAsync(stoppingToken);
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

        public async Task RemoveGlobalConfig(string key)
        {
            var data = new GlobalConfig { Key = key };
            _context.Remove(data);
            await _context.SaveChangesAsync();
        }
    }
}