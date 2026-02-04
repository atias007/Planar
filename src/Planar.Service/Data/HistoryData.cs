using Dapper;
using Microsoft.EntityFrameworkCore;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.Data.Scripts.Sqlite;
using Planar.Service.Model;
using Planar.Service.Model.DataObjects;
using Polly;
using Quartz;
using RepoDb;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Data;

public interface IHistoryData : IBaseDataLayer
{
    Task ClearHistoryLastLogs(IEnumerable<string> jobIds);

    Task ClearJobHistory(string jobId);

    Task ClearJobHistory(IEnumerable<string> jobIds);

    Task<int> ClearJobLogTable(int overDays, int batchSize);

    Task<int> ClearJobLogTable(string jobId, int overDays, int batchSize);

    Task CreateJobInstanceLog(JobInstanceLog log);

    IQueryable<JobInstanceLog> GetHistory(GetHistoryRequest request);

    IQueryable<JobInstanceLog> GetHistory(long key);

    Task<JobInstanceLog?> GetHistoryById(long id);

    Task<JobInstanceLog?> GetHistoryByInstanceId(string instanceid);

    Task<HistoryStatusDto?> GetHistoryCounter(CounterRequest counterRequest);

    IQueryable<JobInstanceLog> GetHistoryData();

    Task<DbDataReader> GetHistoryDataById(long id);

    Task<DbDataReader> GetHistoryDataByInstanceId(string instanceid);

    Task<DbDataReader> GetHistoryExceptionById(long id);

    Task<DbDataReader> GetHistoryExceptionByInstanceId(string instanceid);

    Task<IEnumerable<string>> GetHistoryJobIds();

    Task<DbDataReader> GetHistoryLogById(long id);

    Task<DbDataReader> GetHistoryLogByInstanceId(string instanceid);

    Task<int?> GetHistoryStatusById(long id);

    Task<(IEnumerable<HistorySummary>, int)> GetHistorySummary(GetSummaryRequest request);

    Task<PagingResponse<HistoryLastLog>> GetLastHistoryCallForJob(GetLastHistoryCallForJobRequest request);

    Task<IEnumerable<string>> GetLastHistoryJobIds();

    Task<LastInstanceId?> GetLastInstanceId(JobKey jobKey, DateTime invokeDateTime, CancellationToken cancellationToken);

    Task<bool> IsHistoryExists(long id);

    Task MergeHistoryLastLog(HistoryLastLog log);

    Task PersistJobInstanceData(JobInstanceLog log);

    Task SetAnomaly(JobInstanceLog jobInstance);

    Task SetJobInstanceLogStatus(string instanceId, StatusMembers status);

    Task UpdateHistoryJobRunLog(JobInstanceLog log);
}

public class HistoryDataSqlite(PlanarContext context) : HistoryData(context), IHistoryData
{
}

public class HistoryDataSqlServer(PlanarContext context) : HistoryData(context), IHistoryData
{
}

public class HistoryData(PlanarContext context) : BaseDataLayer(context)
{
    public async Task<(IEnumerable<HistorySummary>, int)> GetHistorySummary(GetSummaryRequest request)
    {
        var fromDate = request.FromDate;
        var toDate = request.ToDate;
        var pageNumber = request.PageNumber.GetValueOrDefault();
        var pageSize = request.PageSize.GetValueOrDefault();

        var summary = await _context.JobInstanceLogs
            .Where(log => (fromDate == null || log.StartDate > fromDate) &&
                          (toDate == null || log.StartDate <= toDate))
            .GroupBy(log => new { log.JobId, log.JobName, log.JobGroup, log.JobType })
            .Select(group => new HistorySummary
            {
                JobId = group.Key.JobId,
                JobName = group.Key.JobName,
                JobGroup = group.Key.JobGroup,
                JobType = group.Key.JobType,
                Total = group.Count(),
                Success = group.Sum(log => log.Status == 0 ? 1 : 0),
                Fail = group.Sum(log => log.Status == 1 ? 1 : 0),
                Running = group.Sum(log => log.Status == -1 ? 1 : 0),
                Retries = group.Sum(log => log.Retry ? 1 : 0),
                TotalEffectedRows = group.Sum(log => log.EffectedRows.GetValueOrDefault())
            })
            .OrderBy(item => item.JobGroup)
            .ThenBy(item => item.JobName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Second SQL Query (converted to LINQ) - Counting the total number of groups
        var totalRuns = await _context.JobInstanceLogs
            .Where(log => (fromDate == null || log.StartDate > fromDate) &&
                          (toDate == null || log.StartDate <= toDate))
            .GroupBy(log => new { log.JobId, log.JobName, log.JobGroup, log.JobType })
            .CountAsync();

        return (summary, totalRuns);
    }

    public async Task<int> ClearJobLogTable(int overDays, int batchSize)
    {
        var referenceDate = DateTime.Now.Date.AddDays(-overDays);
        _context.Database.SetCommandTimeout(600); // 10 minutes
        var result = await _context.JobInstanceLogs
            .Where(l => l.StartDate < referenceDate)
            .OrderBy(l => l.Id)
            .Take(batchSize)
            .ExecuteDeleteAsync();

        return result;
    }

    public async Task<int> ClearJobLogTable(string jobId, int overDays, int batchSize)
    {
        var referenceDate = DateTime.Now.Date.AddDays(-overDays);
        _context.Database.SetCommandTimeout(600); // 10 minutes
        var result = await _context.JobInstanceLogs
            .Where(l => l.StartDate < referenceDate && l.JobId == jobId)
            .OrderBy(l => l.Id)
            .Take(batchSize)
            .ExecuteDeleteAsync();

        return result;
    }

    public async Task ClearHistoryLastLogs(IEnumerable<string> jobIds)
    {
        await _context.HistoryLastLogs
            .Where(h => jobIds.Contains(h.JobId))
            .ExecuteDeleteAsync();
    }

    public async Task ClearJobHistory(string jobId)
    {
        await _context.JobInstanceLogs
            .Where(l => l.JobId == jobId)
            .ExecuteDeleteAsync();

        await _context.HistoryLastLogs
            .Where(l => l.JobId == jobId)
            .ExecuteDeleteAsync();
    }

    public async Task ClearJobHistory(IEnumerable<string> jobIds)
    {
        await _context.JobInstanceLogs
            .Where(h => jobIds.Contains(h.JobId))
            .ExecuteDeleteAsync();
    }

    public async Task CreateJobInstanceLog(JobInstanceLog log)
    {
        _context.JobInstanceLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public IQueryable<JobInstanceLog> GetHistory(long key)
    {
        return _context.JobInstanceLogs.AsNoTracking().Where(l => l.Id == key);
    }

    public IQueryable<JobInstanceLog> GetHistory(GetHistoryRequest request)
    {
        var query = _context.JobInstanceLogs.AsNoTracking();

        if (request.FromDate.HasValue)
        {
            query = query.Where(l => l.StartDate >= request.FromDate);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(l => l.StartDate < request.ToDate);
        }

        if (request.Status != null)
        {
            query = query.Where(l => l.Status == (int)request.Status);
        }

        if (!string.IsNullOrEmpty(request.JobId))
        {
            var index = request.JobId.IndexOf('.');
            if (index > 0)
            {
                var group = request.JobId[0..index];
                var name = request.JobId[(index + 1)..];
                query = query.Where(l => l.JobGroup == group && l.JobName == name);
            }
            else
            {
                query = query.Where(l => l.JobId == request.JobId);
            }
        }

        if (!string.IsNullOrEmpty(request.JobGroup))
        {
            query = query.Where(l => l.JobGroup == request.JobGroup);
        }

        if (!string.IsNullOrEmpty(request.JobType))
        {
            query = query.Where(l => l.JobType == request.JobType);
        }

        if (request.Outlier.HasValue && request.Outlier.Value)
        {
            query = query.Where(l => l.Anomaly > 0);
        }

        if (request.Outlier.HasValue && !request.Outlier.Value)
        {
            query = query.Where(l => l.Anomaly == 0);
        }

        if (request.HasWarnings.HasValue)
        {
            query = query.Where(l => l.HasWarnings == request.HasWarnings.Value);
        }

        if (request.Ascending)
        {
            query = query.OrderBy(l => l.StartDate);
        }
        else
        {
            query = query.OrderByDescending(l => l.StartDate);
        }

        return query;
    }

    public async Task<JobInstanceLog?> GetHistoryById(long id)
    {
        var result = await _context.JobInstanceLogs
            .AsNoTracking()
            .Where(l => l.Id == id)
            .Select(h => new JobInstanceLog
            {
                Id = h.Id,
                JobId = h.JobId,
                JobName = h.JobName,
                JobGroup = h.JobGroup,
                JobType = h.JobType,
                InstanceId = h.InstanceId,
                StartDate = h.StartDate,
                EndDate = h.EndDate,
                Duration = h.Duration,
                Status = h.Status,
                StatusTitle = h.StatusTitle,
                Data = h.Data == null ? null : h.Data.Substring(0, JobHistory.DataMaximumLength + 1),
                Log = h.Log == null ? null : h.Log.Substring(0, JobHistory.LogMaximumLength + 1),
                Exception = h.Exception == null ? null : h.Exception.Substring(0, JobHistory.LogMaximumLength + 1),
                ExceptionCount = h.ExceptionCount,
                EffectedRows = h.EffectedRows,
                IsCanceled = h.IsCanceled,
                Anomaly = h.Anomaly,
                HasWarnings = h.HasWarnings,
                TriggerId = h.TriggerId,
                TriggerName = h.TriggerName,
                TriggerGroup = h.TriggerGroup,
                ServerName = h.ServerName,
                Retry = h.Retry
            })
            .FirstOrDefaultAsync();

        return result;
    }

    public async Task<JobInstanceLog?> GetHistoryByInstanceId(string instanceid)
    {
        var result = await _context.JobInstanceLogs
            .AsNoTracking()
            .Where(l => l.InstanceId == instanceid)
            .FirstOrDefaultAsync();

        return result;
    }

    public async Task<HistoryStatusDto?> GetHistoryCounter(CounterRequest counterRequest)
    {
        var query = from log in _context.JobInstanceLogs
                    where (counterRequest.FromDate == null || log.StartDate > counterRequest.FromDate) &&
                          (counterRequest.ToDate == null || log.StartDate <= counterRequest.ToDate)
                    group log by 1 into g
                    select new HistoryStatusDto
                    {
                        Running = g.Count(x => x.Status == -1),
                        Success = g.Count(x => x.Status == 0),
                        Fail = g.Count(x => x.Status == 1)
                    };

        var result = await query.OrderBy(q => q.Success).FirstOrDefaultAsync();
        return result;
    }

    public IQueryable<JobInstanceLog> GetHistoryData()
    {
        return _context.JobInstanceLogs
            .AsNoTracking()
            .OrderByDescending(l => l.StartDate)
            .AsQueryable();
    }

    public async Task<DbDataReader> GetHistoryDataById(long id)
    {
        const string query = "SELECT Data FROM JobInstanceLog WHERE Id = @Id";

        var def = new CommandDefinition
        (
            commandText: query,
            parameters: new { Id = id },
            commandType: CommandType.Text
        );

        var connection = _context.Database.GetDbConnection();
        var reader = await connection.ExecuteReaderAsync(def, commandBehavior: CommandBehavior.SequentialAccess);
        return reader;
    }

    public async Task<DbDataReader> GetHistoryDataByInstanceId(string instanceid)
    {
        const string query = "SELECT Data FROM JobInstanceLog WHERE InstanceId = @InstanceId";

        var def = new CommandDefinition
        (
            commandText: query,
            parameters: new { InstanceId = instanceid },
            commandType: CommandType.Text
        );

        var connection = _context.Database.GetDbConnection();
        var reader = await connection.ExecuteReaderAsync(def, commandBehavior: CommandBehavior.SequentialAccess);
        return reader;
    }

    public async Task<DbDataReader> GetHistoryExceptionById(long id)
    {
        const string query = "SELECT Exception FROM JobInstanceLog WHERE Id = @Id";
        var def = new CommandDefinition
        (
            commandText: query,
            parameters: new { Id = id },
            commandType: CommandType.Text
        );

        var connection = _context.Database.GetDbConnection();
        var reader = await connection.ExecuteReaderAsync(def, commandBehavior: CommandBehavior.SequentialAccess);
        return reader;
    }

    public async Task<DbDataReader> GetHistoryExceptionByInstanceId(string instanceid)
    {
        const string query = "SELECT Exception FROM JobInstanceLog WHERE InstanceId = @InstanceId";
        var def = new CommandDefinition
        (
            commandText: query,
            parameters: new { InstanceId = instanceid },
            commandType: CommandType.Text
        );

        var connection = _context.Database.GetDbConnection();
        var reader = await connection.ExecuteReaderAsync(def, commandBehavior: CommandBehavior.SequentialAccess);
        return reader;
    }

    public async Task<IEnumerable<string>> GetHistoryJobIds()
    {
        return await _context.JobInstanceLogs
            .Select(l => l.JobId)
            .Distinct()
            .ToListAsync();
    }

    public async Task<DbDataReader> GetHistoryLogById(long id)
    {
        const string query = "SELECT Log FROM JobInstanceLog WHERE Id = @Id";
        var def = new CommandDefinition
        (
            commandText: query,
            parameters: new { Id = id },
            commandType: CommandType.Text
        );

        var connection = _context.Database.GetDbConnection();
        var reader = await connection.ExecuteReaderAsync(def, commandBehavior: CommandBehavior.SequentialAccess);
        return reader;
    }

    public async Task<DbDataReader> GetHistoryLogByInstanceId(string instanceid)
    {
        const string query = "SELECT Log FROM JobInstanceLog WHERE InstanceId = @InstanceId";
        var def = new CommandDefinition
        (
            commandText: query,
            parameters: new { InstanceId = instanceid },
            commandType: CommandType.Text
        );

        var connection = _context.Database.GetDbConnection();
        var reader = await connection.ExecuteReaderAsync(def, commandBehavior: CommandBehavior.SequentialAccess);
        return reader;
    }

    public async Task<int?> GetHistoryStatusById(long id)
    {
        var result = await _context.JobInstanceLogs
            .AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => l.Status)
            .FirstOrDefaultAsync();

        return result;
    }

    public async Task<PagingResponse<HistoryLastLog>> GetLastHistoryCallForJob(GetLastHistoryCallForJobRequest request)
    {
        var last = _context.HistoryLastLogs.AsNoTracking();
        if (request.JobId.HasValue()) { last = last.Where(l => l.JobId == request.JobId); }
        if (request.JobGroup.HasValue()) { last = last.Where(l => l.JobGroup == request.JobGroup); }
        if (request.JobType.HasValue()) { last = last.Where(l => l.JobType == request.JobType); }
        if (request.LastDays.HasValue)
        {
            var referenceDate = DateTime.Now.Date.AddDays(-request.LastDays.Value);
            last = last.Where(l => l.StartDate >= referenceDate);
        }

        var result = await last
            .OrderByDescending(l => l.StartDate)
            .ToPagingListAsync(request);

        return result;
    }

    public async Task<IEnumerable<string>> GetLastHistoryJobIds()
    {
        return await _context.HistoryLastLogs
          .Select(l => l.JobId)
          .Distinct()
          .ToListAsync();
    }

    public async Task<LastInstanceId?> GetLastInstanceId(JobKey jobKey, DateTime invokeDateTime, CancellationToken cancellationToken)
    {
        var result = await _context.JobInstanceLogs
            .Where(l =>
                l.JobName == jobKey.Name &&
                l.JobGroup == jobKey.Group &&
                l.StartDate >= invokeDateTime &&
                l.TriggerId == Consts.ManualTriggerId)
            .OrderByDescending(l => l.StartDate)
            .Select(l => new LastInstanceId
            {
                InstanceId = l.InstanceId,
                LogId = l.Id,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return result;
    }

    public async Task<bool> IsHistoryExists(long id)
    {
        var result = await _context.JobInstanceLogs
            .AnyAsync(l => l.Id == id);

        return result;
    }

    public async Task MergeHistoryLastLog(HistoryLastLog log)
    {
        var id = await _context.JobInstanceLogs.Where(l => l.InstanceId == log.InstanceId).Select(l => l.Id).FirstOrDefaultAsync();
        log.Id = id;
        await _context.HistoryLastLogs.Where(l => l.JobId == log.JobId).ExecuteDeleteAsync();
        _context.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task PersistJobInstanceData(JobInstanceLog log)
    {
        await _context.JobInstanceLogs
            .Where(l => l.InstanceId == log.InstanceId && l.Status == -1)
            .ExecuteUpdateAsync(u => u
                .SetProperty(l => l.Log, log.Log)
                .SetProperty(l => l.Exception, log.Exception)
                .SetProperty(l => l.Duration, log.Duration)
                );
    }

    public async Task SetAnomaly(JobInstanceLog jobInstance)
    {
        await _context.JobInstanceLogs
            .Where(l => l.InstanceId == jobInstance.InstanceId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(l => l.Anomaly, jobInstance.Anomaly));
    }

    public async Task SetJobInstanceLogStatus(string instanceId, StatusMembers status)
    {
        await _context.JobInstanceLogs
            .Where(l => l.InstanceId == instanceId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(l => l.Status, (int)status)
                .SetProperty(l => l.StatusTitle, status.ToString()));
    }

    public async Task UpdateHistoryJobRunLog(JobInstanceLog log)
    {
        await _context.JobInstanceLogs
            .Where(l => l.InstanceId == log.InstanceId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(l => l.Status, log.Status)
                .SetProperty(l => l.StatusTitle, log.StatusTitle)
                .SetProperty(l => l.EndDate, log.EndDate)
                .SetProperty(l => l.Duration, log.Duration)
                .SetProperty(l => l.EffectedRows, log.EffectedRows)
                .SetProperty(l => l.Log, log.Log)
                .SetProperty(l => l.Exception, log.Exception)
                .SetProperty(l => l.ExceptionCount, log.ExceptionCount)
                .SetProperty(l => l.IsCanceled, log.IsCanceled)
                .SetProperty(l => l.HasWarnings, log.HasWarnings)
            );
    }
}