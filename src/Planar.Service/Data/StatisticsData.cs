using Dapper;
using Microsoft.EntityFrameworkCore;
using Planar.Service.Model;
using System.Data;
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

        public async Task ClearStatisticsTables(int overDays)
        {
            var parameters = new { OverDays = overDays };
            using var conn = _context.Database.GetDbConnection();
            var cmd = new CommandDefinition(
                commandText: "Statistics.ClearStatistics",
                commandType: CommandType.StoredProcedure,
                parameters: parameters);

            await conn.ExecuteAsync(cmd);
        }
    }
}