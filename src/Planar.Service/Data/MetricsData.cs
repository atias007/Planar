using Dapper;
using Microsoft.EntityFrameworkCore;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.Data.Scripts.Sqlite;
using Planar.Service.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.Data;

public interface IMetricsData : IBaseDataLayer
{
    Task AddCocurentQueueItem(ConcurrentQueue item);

    Task<int> BuildJobStatistics();

    Task<int> ClearStatisticsTables(int overDays);

    Task DeleteJobStatistic(JobDurationStatistic item);

    Task DeleteJobStatistic(JobEffectedRowsStatistic item);

    Task<int> FillJobCounters();

    Task<JobCounters?> GetAllJobsCounters(DateTime? fromDate);

    IQueryable<ConcurrentExecution> GetConcurrentExecution(ConcurrentExecutionRequest request);

    Task<JobCounters?> GetJobCounters(string id);

    Task<IEnumerable<JobDurationStatistic>> GetJobDurationStatistics();

    IQueryable<JobDurationStatistic> GetJobDurationStatistics(string jobId);

    Task<IEnumerable<string>> GetJobDurationStatisticsIds();

    Task<IEnumerable<JobEffectedRowsStatistic>> GetJobEffectedRowsStatistics();

    IQueryable<JobEffectedRowsStatistic> GetJobEffectedRowsStatistics(string jobId);

    Task<IEnumerable<string>> GetJobEffectedRowsStatisticsIds();

    Task<int> GetMaxConcurrentExecution(MaxConcurrentExecutionRequest request);

    IQueryable<JobInstanceLog> GetNullAnomaly();

    void SetAnomaly(IEnumerable<JobInstanceLog> logs);

    Task<int> SetMaxConcurrentExecution();
}

public class MetricsDataSqlite(PlanarContext context) : MetricsData(context), IMetricsData
{
    public async Task<int> BuildJobStatistics()
    {
        var cmd = new CommandDefinition(
            commandText: SqliteResource.GetScript("BuildJobStatistics"),
            commandType: CommandType.Text);

        return await DbConnection.ExecuteAsync(cmd);
    }

    public async Task<int> FillJobCounters()
    {
        var cmd = new CommandDefinition(
            commandText: SqliteResource.GetScript("FillJobCounters"),
            commandType: CommandType.Text);

        return await DbConnection.ExecuteAsync(cmd);
    }

    public async Task<int> SetMaxConcurrentExecution()
    {
        var hour = DateTime.Now.Hour;
        var current = DateTime.Now.Date.AddHours(hour);

        var cmd = new CommandDefinition(
            commandText: SqliteResource.GetScript("SetMaxConcurrentExecution"),
            parameters: new { current },
            commandType: CommandType.Text);

        return await DbConnection.ExecuteAsync(cmd);
    }
}

public class MetricsDataSqlServer(PlanarContext context) : MetricsData(context), IMetricsData
{
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

    public async Task<int> SetMaxConcurrentExecution()
    {
        var cmd = new CommandDefinition(
            commandText: "Statistics.SetMaxConcurrentExecution",
            commandType: CommandType.StoredProcedure);

        return await DbConnection.ExecuteAsync(cmd);
    }
}

public class MetricsData(PlanarContext context) : BaseDataLayer(context)
{
    public async Task<int> ClearStatisticsTables(int overDays)
    {
        var referenceDate = DateTime.Now.Date.AddDays(-overDays);

        var count = await _context.ConcurrentQueues
            .Where(l => l.RecordDate < referenceDate)
            .ExecuteDeleteAsync();

        count += await _context.ConcurrentExecutions
            .Where(l => l.RecordDate < referenceDate)
            .ExecuteDeleteAsync();

        return count;
    }

    public async Task AddCocurentQueueItem(ConcurrentQueue item)
    {
        _context.Add(item);
        await SaveChangesAsync();
    }

    public async Task DeleteJobStatistic(JobDurationStatistic item)
    {
        await _context.JobDurationStatistics
            .Where(i => i.JobId == item.JobId)
            .ExecuteDeleteAsync();
    }

    public async Task DeleteJobStatistic(JobEffectedRowsStatistic item)
    {
        await _context.JobEffectedRowsStatistics
            .Where(i => i.JobId == item.JobId)
            .ExecuteDeleteAsync();
    }

    public async Task<JobCounters?> GetAllJobsCounters(DateTime? fromDate)
    {
        var query = _context.JobCounters.AsNoTracking();

        if (fromDate.HasValue)
        {
            query = query.Where(j => j.RunDate >= DateOnly.FromDateTime(fromDate.Value));
        }

        var groupQuery = query.GroupBy(j => 1)  // Group by a constant to get aggregate counts
            .Select(g => new JobCounters
            {
                TotalRuns = g.Sum(j => j.TotalRuns),
                SuccessRetries = g.Sum(j => j.SuccessRetries) ?? 0,
                FailRetries = g.Sum(j => j.FailRetries) ?? 0,
                Recovers = g.Sum(j => j.Recovers) ?? 0
            });

        var result = await groupQuery.SingleOrDefaultAsync();
        return result;
    }

    public IQueryable<ConcurrentExecution> GetConcurrentExecution(ConcurrentExecutionRequest request)
    {
        var query = _context.ConcurrentExecutions.AsNoTracking();

        if (request.FromDate.HasValue) { query = query.Where(c => c.RecordDate >= request.FromDate.Value); }
        if (request.ToDate.HasValue) { query = query.Where(c => c.RecordDate < request.ToDate.Value); }
        if (request.Server.HasValue()) { query = query.Where(c => c.Server == request.Server); }
        if (request.InstanceId.HasValue()) { query = query.Where(c => c.InstanceId == request.InstanceId); }

        query = query.OrderByDescending(c => c.RecordDate);
        return query;
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

    public async Task<IEnumerable<string>> GetJobDurationStatisticsIds()
    {
        return await _context.JobDurationStatistics
            .AsNoTracking()
            .Select(j => j.JobId)
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

    public async Task<IEnumerable<string>> GetJobEffectedRowsStatisticsIds()
    {
        return await _context.JobEffectedRowsStatistics
            .AsNoTracking()
            .Select(j => j.JobId)
            .ToListAsync();
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
            .Where(j => j.Anomaly == null && j.Status != -1);
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