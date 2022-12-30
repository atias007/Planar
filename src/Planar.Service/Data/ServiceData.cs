using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Planar.Service.Data
{
    public class ServiceData : BaseDataLayer
    {
        public ServiceData(PlanarContext context) : base(context)
        {
        }

        public async Task HealthCheck()
        {
            const string query = "SELECT 1";
            await _context.Database.ExecuteSqlRawAsync(query);
        }
    }
}