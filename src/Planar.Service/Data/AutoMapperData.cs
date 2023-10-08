using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.Data
{
    public class AutoMapperData : BaseDataLayer
    {
        public AutoMapperData(PlanarContext context) : base(context)
        {
        }

        public async Task<int> GetGroupId(string name)
        {
            return await _context.Groups
                .AsNoTracking()
                .Where(g => g.Name.ToLower() == name.ToLower())
                .Select(g => g.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<string?> GetGroupName(int id)
        {
            return await _context.Groups
                .AsNoTracking()
                .Where(g => g.Id == id)
                .Select(g => g.Name)
                .FirstOrDefaultAsync();
        }
    }
}