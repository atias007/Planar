using Dapper;
using Microsoft.EntityFrameworkCore;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.General;
using Planar.Service.Model;
using Polly;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.Data
{
    public class StatisticsData : BaseDataLayer
    {
        public StatisticsData(PlanarContext context) : base(context)
        {
        }

        public IQueryable<JobDurationStatistic> GetJobDurationStatistics(string jobId)
        {
            return _context.JobDurationStatistics
                .AsNoTracking()
                .Where(job => job.JobId == jobId);
        }

        public async Task<IEnumerable<JobDurationStatistic>> GetJobDurationStatistics()
        {
            return await _context.JobDurationStatistics
                .AsNoTracking()
                .ToListAsync();
        }

        public IQueryable<JobEffectedRowsStatistic> GetJobEffectedRowsStatistics(string jobId)
        {
            return _context.JobEffectedRowsStatistics
                .AsNoTracking()
                .Where(job => job.JobId == jobId);
        }

        public async Task<IEnumerable<JobEffectedRowsStatistic>> GetJobEffectedRowsStatistics()
        {
            return await _context.JobEffectedRowsStatistics
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AddCocurentQueueItem(ConcurrentQueue item)
        {
            _context.Add(item);
            await SaveChangesAsync();
        }

        public async Task<int> ClearStatisticsTables(int overDays)
        {
            var parameters = new { OverDays = overDays };
            var cmd = new CommandDefinition(
                commandText: "Statistics.ClearStatistics",
                commandType: CommandType.StoredProcedure,
                parameters: parameters);

            return await DbConnection.ExecuteAsync(cmd);
        }

        public async Task<int> SetMaxConcurrentExecution()
        {
            var cmd = new CommandDefinition(
                commandText: "Statistics.SetMaxConcurrentExecution",
                commandType: CommandType.StoredProcedure);

            return await DbConnection.ExecuteAsync(cmd);
        }

        public async Task<int> BuildJobStatistics()
        {
            var cmd = new CommandDefinition(
                commandText: "Statistics.BuildJobStatistics",
                commandType: CommandType.StoredProcedure);

            return await DbConnection.ExecuteAsync(cmd);
        }

        public async Task<int> FillJobCounters()
        {
            var cmd = new CommandDefinition(
                commandText: "Statistics.FillJobCounters",
                commandType: CommandType.StoredProcedure);

            return await DbConnection.ExecuteAsync(cmd);
        }

        public async Task<IEnumerable<string>> GetJobDurationStatisticsIds()
        {
            return await _context.JobDurationStatistics
                .AsNoTracking()
                .Select(j => j.JobId)
                .ToListAsync();
        }

        public async Task<IEnumerable<string>> GetJobEffectedRowsStatisticsIds()
        {
            return await _context.JobEffectedRowsStatistics
                .AsNoTracking()
                .Select(j => j.JobId)
                .ToListAsync();
        }

        public async Task DeleteJobStatistic(JobDurationStatistic item)
        {
            await _context.JobDurationStatistics.Where(i => i.JobId == item.JobId).ExecuteDeleteAsync();
        }

        public async Task DeleteJobStatistic(JobEffectedRowsStatistic item)
        {
            await _context.JobEffectedRowsStatistics.Where(i => i.JobId == item.JobId).ExecuteDeleteAsync();
        }

        public async Task<JobCounters?> GetJobCounters(string id)
        {
            var result = await _context.JobCounters
                .AsNoTracking()
                .Where(j => j.JobId == id)
                .GroupBy(j => 1)  // Group by a constant to get aggregate counts
                .Select(g => new JobCounters
                {
                    TotalRuns = g.Sum(j => j.TotalRuns),
                    SuccessRetries = g.Sum(j => j.SuccessRetries) ?? 0,
                    FailRetries = g.Sum(j => j.FailRetries) ?? 0,
                    Recovers = g.Sum(j => j.Recovers) ?? 0
                })
                .SingleOrDefaultAsync();

            return result;
        }

        public async Task<JobCounters?> GetAllJobsCounters(DateTime fromDate)
        {
            var result = await _context.JobCounters
                .AsNoTracking()
                .Where(j => j.RunDate >= fromDate)
                .GroupBy(j => 1)  // Group by a constant to get aggregate counts
                .Select(g => new JobCounters
                {
                    TotalRuns = g.Sum(j => j.TotalRuns),
                    SuccessRetries = g.Sum(j => j.SuccessRetries) ?? 0,
                    FailRetries = g.Sum(j => j.FailRetries) ?? 0,
                    Recovers = g.Sum(j => j.Recovers) ?? 0
                })
                .SingleOrDefaultAsync();

            return result;
        }

        public IQueryable<ConcurrentExecution> GetConcurrentExecution(ConcurrentExecutionRequest request)
        {
            var query = _context.ConcurrentExecutions.AsNoTracking();

            if (request.FromDate.HasValue) { query = query.Where(c => c.RecordDate >= request.FromDate.Value); }
            if (request.ToDate.HasValue) { query = query.Where(c => c.RecordDate < request.ToDate.Value); }
            if (request.Server.HasValue()) { query = query.Where(c => c.Server.ToLower() == request.Server.ToLower()); }
            if (request.InstanceId.HasValue()) { query = query.Where(c => c.InstanceId.ToLower() == request.InstanceId.ToLower()); }

            query = query.OrderByDescending(c => c.RecordDate).Take(1000);
            return query;
        }

        public async Task<int> GetMaxConcurrentExecution(MaxConcurrentExecutionRequest request)
        {
            var query = _context.ConcurrentExecutions.AsNoTracking();

            if (request.FromDate.HasValue) { query = query.Where(c => c.RecordDate >= request.FromDate.Value); }
            if (request.ToDate.HasValue) { query = query.Where(c => c.RecordDate < request.ToDate.Value); }

            var result = await query
                .Select(c => c.MaxConcurrent)
                .DefaultIfEmpty()
                .MaxAsync();

            return result;
        }

        public IQueryable<JobInstanceLog> GetNullAnomaly()
        {
            return _context.JobInstanceLogs
                .AsNoTracking()
                .Where(j => j.Anomaly == null);
        }

        public void SetAnomaly(IEnumerable<JobInstanceLog> logs)
        {
            foreach (var log in logs)
            {
                _context.Attach(log);
                _context.Entry(log).Property(l => l.Anomaly).IsModified = true;
            }
        }
    }
}