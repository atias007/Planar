using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.Model;
using Quartz;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Data
{
    public class DataLayer : BaseDataLayer, IJobPropertyDataLayer
    {
        public DataLayer(PlanarContext context) : base(context)
        {
        }

        public IQueryable<Trace> GetTraceData()
        {
            return _context.Traces.AsQueryable();
        }

        public IQueryable<JobInstanceLog> GetHistoryData()
        {
            return _context.JobInstanceLogs.AsQueryable();
        }

        public async Task HealthCheck()
        {
            const string query = "SELECT 1";
            await _context.Database.ExecuteSqlRawAsync(query);
        }

        public async Task<int> SaveChanges()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task CreateJobInstanceLog(JobInstanceLog log)
        {
            _context.JobInstanceLogs.Add(log);
            await _context.SaveChangesAsync();
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
                log.IsStopped
            };

            var cmd = new CommandDefinition(
                commandText: "dbo.UpdateJobInstanceLog",
                commandType: CommandType.StoredProcedure,
                parameters: parameters);

            await _context.Database.GetDbConnection().ExecuteAsync(cmd);
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

            await _context.Database.GetDbConnection().ExecuteAsync(cmd);
        }

        public async Task SetJobInstanceLogStatus(string instanceId, StatusMembers status)
        {
            var paramInstanceId = new SqlParameter("@InstanceId", instanceId);
            var paramStatus = new SqlParameter("@Status", (int)status);
            var paramTitle = new SqlParameter("@StatusTitle", status.ToString());

            await _context.Database.ExecuteSqlRawAsync($"dbo.SetJobInstanceLogStatus @InstanceId,  @Status, @StatusTitle", paramInstanceId, paramStatus, paramTitle);
        }

        public Trace GetTrace(int key)
        {
            return _context.Traces.Find(key);
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

            if (string.IsNullOrEmpty(request.Level) == false)
            {
                query = query.Where(l => l.Level == request.Level);
            }

            if (request.Ascending)
            {
                query = query.OrderBy(l => l.TimeStamp);
            }
            else
            {
                query = query.OrderByDescending(l => l.TimeStamp);
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

        public async Task<string> GetTraceException(int id)
        {
            var result = (await _context.Traces.FindAsync(id))?.Exception;
            return result;
        }

        public async Task<string> GetTraceProperties(int id)
        {
            var result = (await _context.Traces.FindAsync(id))?.LogEvent;
            return result;
        }

        public async Task<bool> IsTraceExists(int id)
        {
            return await _context.Traces.AnyAsync(t => t.Id == id);
        }

        public async Task<GlobalConfig> GetGlobalConfig(string key)
        {
            var result = await _context.GlobalConfigs.FindAsync(key);
            return result;
        }

        public async Task<bool> IsGlobalConfigExists(string key)
        {
            var result = await _context.GlobalConfigs.AnyAsync(p => p.Key == key);
            return result;
        }

        public async Task<IEnumerable<GlobalConfig>> GetAllGlobalConfig(CancellationToken stoppingToken = default)
        {
            var result = await _context.GlobalConfigs.OrderBy(p => p.Key).ToListAsync(stoppingToken);
            return result;
        }

        public async Task AddGlobalConfig(GlobalConfig config)
        {
            _context.GlobalConfigs.Add(config);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateGlobalConfig(GlobalConfig config)
        {
            _context.GlobalConfigs.Update(config);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveGlobalConfig(string key)
        {
            var data = new GlobalConfig { Key = key };
            _context.Remove(data);
            await _context.SaveChangesAsync();
        }

        public async Task<LastInstanceId> GetLastInstanceId(JobKey jobKey, DateTime invokeDateTime)
        {
            var result = await _context.JobInstanceLogs
                .Where(l => l.JobName == jobKey.Name && l.JobGroup == jobKey.Group && l.StartDate >= invokeDateTime && l.TriggerId == Consts.ManualTriggerId)
                .Select(l => new LastInstanceId
                {
                    InstanceId = l.InstanceId,
                    LogId = l.Id,
                })
                .FirstOrDefaultAsync();

            return result;
        }

        public async Task<List<JobInstanceLog>> GetLastHistoryCallForJob(object parameters)
        {
            using var conn = _context.Database.GetDbConnection();
            var cmd = new CommandDefinition(
                commandText: "dbo.GetLastHistoryCallForJob",
                commandType: CommandType.StoredProcedure,
                parameters: parameters);

            var data = await conn.QueryAsync<JobInstanceLog>(cmd);
            return data.ToList();
        }

        public IQueryable<JobInstanceLog> GetHistory(int key)
        {
            return _context.JobInstanceLogs.Where(l => l.Id == key);
        }

        public async Task<List<JobInstanceLog>> GetHistory(GetHistoryRequest request)
        {
            var query = _context.JobInstanceLogs.AsQueryable();

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

            if (request.Ascending)
            {
                query = query.OrderBy(l => l.StartDate);
            }
            else
            {
                query = query.OrderByDescending(l => l.StartDate);
            }

            query = query.Take(request.Rows.GetValueOrDefault());
            var final = query.Select(l => new JobInstanceLog
            {
                Id = l.Id,
                JobId = l.JobId,
                JobName = l.JobName,
                JobGroup = l.JobGroup,
                TriggerId = l.TriggerId,
                Status = l.Status,
                StartDate = l.StartDate,
                Duration = l.Duration,
                EffectedRows = l.EffectedRows
            });

            return await final.ToListAsync();
        }

        public async Task<string> GetHistoryDataById(int id)
        {
            var result = await _context.JobInstanceLogs
                .Where(l => l.Id == id)
                .Select(l => l.Data)
                .FirstOrDefaultAsync();

            return result;
        }

        public async Task<string> GetHistoryLogById(int id)
        {
            var result = await _context.JobInstanceLogs
                .Where(l => l.Id == id)
                .Select(l => l.Log)
                .FirstOrDefaultAsync();

            return result;
        }

        public async Task<string> GetHistoryExceptionById(int id)
        {
            var result = await _context.JobInstanceLogs
                .Where(l => l.Id == id)
                .Select(l => l.Exception)
                .FirstOrDefaultAsync();

            return result;
        }

        public async Task<JobInstanceLog> GetHistoryById(int id)
        {
            var result = await _context.JobInstanceLogs
                .Where(l => l.Id == id)
                .FirstOrDefaultAsync();

            return result;
        }

        public async Task<bool> IsHistoryExists(int id)
        {
            var result = await _context.JobInstanceLogs
                .AnyAsync(l => l.Id == id);

            return result;
        }

        public async Task<GetTestStatusResponse> GetTestStatus(int id)
        {
            var result = await _context.JobInstanceLogs
                .Where(l => l.Id == id)
                .Select(l => new GetTestStatusResponse
                {
                    EffectedRows = l.EffectedRows,
                    Status = l.Status,
                    Duration = l.Duration
                })
                .FirstOrDefaultAsync();

            return result;
        }

        public async Task<int> CountFailsInRowForJob(object parameters)
        {
            using var conn = _context.Database.GetDbConnection();
            var cmd = new CommandDefinition(
                commandText: "dbo.CountFailsInRowForJob",
                commandType: CommandType.StoredProcedure,
                parameters: parameters);

            var data = await conn.QuerySingleAsync<int>(cmd);
            return data;
        }

        public async Task<int> CountFailsInHourForJob(object parameters)
        {
            using var conn = _context.Database.GetDbConnection();
            var cmd = new CommandDefinition(
                commandText: "dbo.CountFailsInHourForJob",
                commandType: CommandType.StoredProcedure,
                parameters: parameters);

            var data = await conn.QuerySingleAsync<int>(cmd);
            return data;
        }

        public async Task ClearTraceTable(int overDays)
        {
            var parameters = new { OverDays = overDays };
            using var conn = _context.Database.GetDbConnection();
            var cmd = new CommandDefinition(
                commandText: "dbo.ClearTrace",
                commandType: CommandType.StoredProcedure,
                parameters: parameters);

            await conn.ExecuteAsync(cmd);
        }

        #region Cluster

        public async Task<ClusterNode> GetClusterNode(ClusterNode item)
        {
            return await _context.ClusterNodes.FirstOrDefaultAsync(c => c.Server == item.Server && c.Port == item.Port);
        }

        public async Task<List<ClusterNode>> GetClusterNodes()
        {
            return await _context.ClusterNodes.ToListAsync();
        }

        public async Task AddClusterNode(ClusterNode item)
        {
            await _context.ClusterNodes.AddAsync(item);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveClusterNode(ClusterNode item)
        {
            _context.ClusterNodes.Remove(item);
            await _context.SaveChangesAsync();
        }

        #endregion Cluster

        #region JobProperty

        public async Task<string> GetJobProperty(string jobId)
        {
            var properties = await _context.JobProperties
                .Where(j => j.JobId == jobId)
                .Select(j => j.Properties)
                .FirstOrDefaultAsync();

            return properties;
        }

        public async Task DeleteJobProperty(string jobId)
        {
            var p = new JobProperty { JobId = jobId };
            _context.JobProperties.Remove(p);
            await SaveChangesWithoutConcurrency();
        }

        public async Task AddJobProperty(JobProperty jobProperty)
        {
            _context.JobProperties.Add(jobProperty);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateJobProperty(JobProperty jobProperty)
        {
            _context.JobProperties.Update(jobProperty);
            await _context.SaveChangesAsync();
        }

        #endregion JobProperty
    }
}