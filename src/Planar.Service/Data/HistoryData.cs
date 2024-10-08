﻿using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Planar.API.Common.Entities;
using Planar.Service.Model;
using Planar.Service.Model.DataObjects;
using Quartz;
using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Data;

public class HistoryData(PlanarContext context) : BaseDataLayer(context)
{
    public async Task<int> ClearJobLogTable(int overDays)
    {
        var parameters = new { OverDays = overDays };
        var cmd = new CommandDefinition(
            commandText: "dbo.ClearLogInstance",
            commandType: CommandType.StoredProcedure,
            parameters: parameters);

        return await DbConnection.ExecuteAsync(cmd);
    }

    public async Task<int> ClearJobLogTable(string jobId, int overDays)
    {
        var parameters = new { JobId = jobId, OverDays = overDays };
        var cmd = new CommandDefinition(
            commandText: "dbo.ClearLogInstanceByJob",
            commandType: CommandType.StoredProcedure,
            parameters: parameters);

        return await DbConnection.ExecuteAsync(cmd);
    }

    public async Task CreateJobInstanceLog(JobInstanceLog log)
    {
        _context.JobInstanceLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<PagingResponse<HistorySummary>> GetHistorySummary(object parameters)
    {
        var cmd = new CommandDefinition(
            commandText: "dbo.GetHistorySummary",
            commandType: CommandType.StoredProcedure,
            parameters: parameters);

        var multi = await DbConnection.QueryMultipleAsync(cmd);
        var data = await multi.ReadAsync<HistorySummary>();
        var count = await multi.ReadSingleAsync<int>();
        return new PagingResponse<HistorySummary>(data.ToList(), count);
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
            .FirstOrDefaultAsync();

        return result;
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
        var definition = new CommandDefinition(
            commandText: "[Statistics].[StatusCounter]",
            parameters: counterRequest,
            commandType: CommandType.StoredProcedure);

        var result = await DbConnection.QueryFirstOrDefaultAsync<HistoryStatusDto>(definition);

        return result;
    }

    public IQueryable<JobInstanceLog> GetHistoryData()
    {
        return _context.JobInstanceLogs.AsNoTracking().OrderByDescending(l => l.StartDate).AsQueryable();
    }

    public async Task<string?> GetHistoryDataById(long id)
    {
        var result = await _context.JobInstanceLogs
            .Where(l => l.Id == id)
            .Select(l => l.Data)
            .FirstOrDefaultAsync();

        return result;
    }

    public async Task<string?> GetHistoryExceptionById(long id)
    {
        var result = await _context.JobInstanceLogs
            .AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => l.Exception)
            .FirstOrDefaultAsync();

        return result;
    }

    public async Task<string?> GetHistoryLogById(long id)
    {
        var result = await _context.JobInstanceLogs
            .AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => l.Log)
            .FirstOrDefaultAsync();

        return result;
    }

    public async Task<PagingResponse<JobLastRun>> GetLastHistoryCallForJob(object parameters)
    {
        var cmd = new CommandDefinition(
            commandText: "dbo.GetLastHistoryCallForJob",
            commandType: CommandType.StoredProcedure,
            parameters: parameters);

        var multi = await DbConnection.QueryMultipleAsync(cmd);
        var data = await multi.ReadAsync<JobLastRun>();
        var count = await multi.ReadSingleAsync<int>();
        return new PagingResponse<JobLastRun>(data.ToList(), count);
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

    public async Task PersistJobInstanceData(JobInstanceLog log)
    {
        var parameters = new
        {
            log.InstanceId,
            log.Log,
            log.Exception,
            log.Duration,
        };

        var cmd = new CommandDefinition(
            commandText: "dbo.PersistJobInstanceLog",
            commandType: CommandType.StoredProcedure,
            parameters: parameters);

        await DbConnection.ExecuteAsync(cmd);
    }

    public async Task SetAnomaly(object parameters)
    {
        var cmd = new CommandDefinition(
            commandText: "dbo.UpdateJobInstanceLogAnomaly",
            commandType: CommandType.StoredProcedure,
            parameters: parameters);

        await DbConnection.ExecuteAsync(cmd);
    }

    public async Task SetJobInstanceLogStatus(string instanceId, StatusMembers status)
    {
        var paramInstanceId = new SqlParameter("@InstanceId", instanceId);
        var paramStatus = new SqlParameter("@Status", (int)status);
        var paramTitle = new SqlParameter("@StatusTitle", status.ToString());

        await _context.Database.ExecuteSqlRawAsync($"dbo.SetJobInstanceLogStatus @InstanceId,  @Status, @StatusTitle", paramInstanceId, paramStatus, paramTitle);
    }

    public async Task UpdateHistoryJobRunLog(JobInstanceLog log)
    {
        var parameters = new
        {
            log.InstanceId,
            log.Status,
            log.StatusTitle,
            log.EndDate,
            log.Duration,
            log.EffectedRows,
            log.Log,
            log.Exception,
            log.ExceptionCount,
            log.IsCanceled,
            log.HasWarnings
        };

        var cmd = new CommandDefinition(
            commandText: "dbo.UpdateJobInstanceLog",
            commandType: CommandType.StoredProcedure,
            parameters: parameters);

        await DbConnection.ExecuteAsync(cmd);
    }
}