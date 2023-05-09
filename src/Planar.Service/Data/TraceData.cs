using Dapper;
using Microsoft.EntityFrameworkCore;
using Planar.API.Common.Entities;
using Planar.Service.Model;
using Planar.Service.Model.DataObjects;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.Data
{
    public class TraceData : BaseDataLayer
    {
        public TraceData(PlanarContext context) : base(context)
        {
        }

        public async Task<int> ClearTraceTable(int overDays)
        {
            var parameters = new { OverDays = overDays };
            using var conn = _context.Database.GetDbConnection();
            var cmd = new CommandDefinition(
                commandText: "dbo.ClearTrace",
                commandType: CommandType.StoredProcedure,
                parameters: parameters);

            return await conn.ExecuteAsync(cmd);
        }

        public async Task<int> ClearJobLogTable(int overDays)
        {
            var parameters = new { OverDays = overDays };
            using var conn = _context.Database.GetDbConnection();
            var cmd = new CommandDefinition(
                commandText: "dbo.ClearLogInstance",
                commandType: CommandType.StoredProcedure,
                parameters: parameters);

            return await conn.ExecuteAsync(cmd);
        }

        public IQueryable<Trace> GetTrace(int key)
        {
            return _context.Traces.Where(t => t.Id == key);
        }

        public async Task<List<LogDetails>> GetTrace(GetTraceRequest request)
        {
            var query = _context.Traces.AsQueryable();

            if (request.FromDate.HasValue)
            {
                query = query.Where(l => l.TimeStamp.LocalDateTime >= request.FromDate);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(l => l.TimeStamp.LocalDateTime < request.ToDate);
            }

            if (!string.IsNullOrEmpty(request.Level))
            {
                query = query.Where(l => l.Level == request.Level);
            }

            if (request.Ascending)
            {
                query = query.OrderBy(l => l.TimeStamp).ThenBy(l => l.Id);
            }
            else
            {
                query = query.OrderByDescending(l => l.TimeStamp).ThenBy(l => l.Id);
            }

            query = query.Take(request.Rows.GetValueOrDefault());

            var final = query.Select(l => new LogDetails
            {
                Id = l.Id,
                Message = l.Message,
                Level = l.Level,
                TimeStamp = l.TimeStamp.ToLocalTime().DateTime
            });

            var result = await final.ToListAsync();
            return result;
        }

        public IQueryable<Trace> GetTraceData()
        {
            return _context.Traces.AsQueryable();
        }

        public async Task<string?> GetTraceException(int id)
        {
            var result = (await _context.Traces.FindAsync(id))?.Exception;
            return result;
        }

        public async Task<string?> GetTraceProperties(int id)
        {
            var result = (await _context.Traces.FindAsync(id))?.LogEvent;
            return result;
        }

        public async Task<bool> IsTraceExists(int id)
        {
            return await _context.Traces.AnyAsync(t => t.Id == id);
        }

        public async Task<TraceStatusDto> GetTraceCounter(int hours)
        {
            var parameters = new { Hours = hours };
            var definition = new CommandDefinition(
                commandText: "[Statistics].[TraceCounter]",
                parameters: parameters,
                commandType: CommandType.StoredProcedure);

            var result = await _context.Database.GetDbConnection()
                .QueryFirstOrDefaultAsync<TraceStatusDto>(definition);

            return result;
        }
    }
}