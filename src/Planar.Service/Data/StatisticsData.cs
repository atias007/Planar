using Planar.Service.Model;
using System.Threading.Tasks;

namespace Planar.Service.Data
{
    public class StatisticsData : BaseDataLayer
    {
        public StatisticsData(PlanarContext context) : base(context)
        {
        }

        public async Task AddCocurentQueueItem(ConcurentQueue item)
        {
            _context.Add(item);
            await SaveChangesAsync();
        }
    }
}