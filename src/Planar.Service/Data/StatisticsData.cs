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

        public async Task<int> ClearStatisticsTables(int overDays)
        {
            var parameters = new { OverDays = overDays };
            using var conn = _context.Database.GetDbConnection();
            var cmd = new CommandDefinition(
                commandText: "Statistics.ClearStatistics",
                commandType: CommandType.StoredProcedure,
                parameters: parameters);

            return await conn.ExecuteAsync(cmd);
        }

        public async Task<int> SetMaxConcurentExecution()
        {
            using var conn = _context.Database.GetDbConnection();
            var cmd = new CommandDefinition(
                commandText: "Statistics.SetMaxConcurentExecution",
                commandType: CommandType.StoredProcedure);

            return await conn.ExecuteAsync(cmd);
        }

        public async Task<int> SetMaxDurationExecution()
        {
            using var conn = _context.Database.GetDbConnection();
            var cmd = new CommandDefinition(
                commandText: "Statistics.SetMaxDurationExecution",
                commandType: CommandType.StoredProcedure);

            return await conn.ExecuteAsync(cmd);
        }

        public async Task<int> BuildJobStatistics()
        {
            using var conn = _context.Database.GetDbConnection();
            var cmd = new CommandDefinition(
                commandText: "Statistics.BuildJobStatistics",
                commandType: CommandType.StoredProcedure);

            return await conn.ExecuteAsync(cmd);
        }
    }
}