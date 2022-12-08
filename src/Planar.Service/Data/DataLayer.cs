using CommonJob;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.Model;
using Planar.Service.Model.DataObjects;
using Quartz;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DbJobInstanceLog = Planar.Service.Model.JobInstanceLog;

using JobInstanceLog = Planar.Service.Model.JobInstanceLog;

namespace Planar.Service.Data
{
    public class DataLayer : IJobPropertyDataLayer
    {
        private readonly PlanarContext _context;

        public DataLayer(PlanarContext context)
        {
            _context = context ?? throw new PlanarJobException(nameof(context));
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

        public async Task CreateJobInstanceLog(DbJobInstanceLog log)
        {
            _context.JobInstanceLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateHistoryJobRunLog(DbJobInstanceLog log)
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

        public async Task PersistJobInstanceData(DbJobInstanceLog log)
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

        #region Monitor

        public async Task<int> GetMonitorCount()
        {
            return await _context.MonitorActions.CountAsync();
        }

        public async Task<List<string>> GetMonitorHooks()
        {
            return await _context.MonitorActions.Select(m => m.Hook).Distinct().ToListAsync();
        }

        public async Task<List<MonitorAction>> GetMonitorData(int @event, string group, string job)
        {
            var all = _context.MonitorActions
                .Include(m => m.Group)
                .ThenInclude(ug => ug.Users);

            var filter = all.Where(m => m.EventId == @event && m.Active == true);
            filter = filter.Where(m =>
                (string.IsNullOrEmpty(m.JobGroup) && string.IsNullOrEmpty(m.JobId)) ||
                (string.IsNullOrEmpty(m.JobGroup) == false && m.JobGroup == group && string.IsNullOrEmpty(m.JobId)) ||
                (string.IsNullOrEmpty(m.JobGroup) && string.IsNullOrEmpty(m.JobId) == false && m.JobId == job));

            return await filter.ToListAsync();
        }

        public async Task<MonitorAction> GetMonitorAction(int id)
        {
            return await _context.MonitorActions.FindAsync(id);
        }

        public async Task<List<MonitorAction>> GetMonitorActions(string jobOrGroupId)
        {
            var query = _context.MonitorActions.AsQueryable();
            if (string.IsNullOrEmpty(jobOrGroupId) == false)
            {
                query = query.Where(m => m.JobId == jobOrGroupId || m.JobGroup == jobOrGroupId);
            }

            query = query.OrderByDescending(m => m.Active)
                .ThenBy(m => m.JobGroup)
                .ThenBy(m => m.JobId);

            var result = await query.Include(m => m.Group).ToListAsync();
            return result;
        }

        public async Task<List<MonitorAction>> GetMonitorActions()
        {
            return await _context.MonitorActions
                .Include(i => i.Group)
                .OrderByDescending(d => d.Active)
                .ThenBy(d => d.JobId)
                .ToListAsync();
        }

        public async Task AddMonitor(MonitorAction request)
        {
            await _context.MonitorActions.AddAsync(request);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteMonitor(MonitorAction request)
        {
            _context.MonitorActions.Remove(request);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteMonitorByJobId(string jobId)
        {
            _context.MonitorActions.RemoveRange(e => e.JobId == jobId);
            await SaveChangesWithoutConcurrency();
        }

        public async Task DeleteMonitorByJobGroup(string jobGroup)
        {
            _context.MonitorActions.RemoveRange(e => e.JobGroup == jobGroup);
            await SaveChangesWithoutConcurrency();
        }

        #endregion Monitor

        private async Task<int> SaveChangesWithoutConcurrency()
        {
            try
            {
                return await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // *** DO NOTHING *** //
            }

            return 0;
        }

        public async Task SetJobInstanceLogStatus(string instanceId, StatusMembers status)
        {
            var paramInstanceId = new SqlParameter("@InstanceId", instanceId);
            var paramStatus = new SqlParameter("@Status", (int)status);
            var paramTitle = new SqlParameter("@StatusTitle", status.ToString());

            await _context.Database.ExecuteSqlRawAsync($"dbo.SetJobInstanceLogStatus @InstanceId,  @Status, @StatusTitle", paramInstanceId, paramStatus, paramTitle);
        }

        public async Task UpdateMonitorAction(MonitorAction monitor)
        {
            _context.MonitorActions.Update(monitor);
            await _context.SaveChangesAsync();
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

        public JobInstanceLog GetHistory(int key)
        {
            return _context.JobInstanceLogs.Find(key);
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

        public async Task<DbJobInstanceLog> GetHistoryById(int id)
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

        public async Task<User> AddUser(User user)
        {
            var result = await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return result.Entity;
        }

        public async Task<User> GetUser(int id)
        {
            var result = await _context.Users.FindAsync(id);
            return result;
        }

        public async Task<User> GetUserByUsername(string username)
        {
            var result = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
            return result;
        }

        public async Task<bool> IsUsernameExists(string username)
        {
            var result = await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower());
            return result;
        }

        internal async Task<List<EntityTitle>> GetGroupsForUser(int id)
        {
            var result = await _context.Groups
                    .Where(g => g.Users.Any(u => u.Id == id))
                    .Select(g => new EntityTitle(g.Id, g.Name))
                    .ToListAsync();

            return result;
        }

        public async Task UpdateUser(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task<List<UserRow>> GetUsers()
        {
            var result = await _context.Users
                .Select(u => new UserRow
                {
                    EmailAddress1 = u.EmailAddress1,
                    FirstName = u.FirstName,
                    Id = u.Id,
                    LastName = u.LastName,
                    PhoneNumber1 = u.PhoneNumber1,
                    Username = u.Username
                })
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            return result;
        }

        public async Task RemoveUser(User user)
        {
            _context.Users.Remove(user);
            _context.SaveChanges();
            await Task.CompletedTask;
        }

        public async Task AddGroup(Group group)
        {
            await _context.Groups.AddAsync(group);
            await _context.SaveChangesAsync();
        }

        internal async Task<List<EntityTitle>> GetUsersInGroup(int id)
        {
            var result = await _context.Users
                .Where(u => u.Groups.Any(g => g.Id == id))
                .Select(u => new EntityTitle(u.Id, u.FirstName, u.LastName))
                .ToListAsync();

            return result;
        }

        public async Task<Group> GetGroup(int id)
        {
            var result = await _context.Groups
                .Include(g => g.Role)
                .FirstOrDefaultAsync(g => g.Id == id);

            return result;
        }

        public async Task<List<GroupInfo>> GetGroups()
        {
            var result = await _context.Groups
                .Include(g => g.Users)
                .Include(g => g.Role)
                .Select(g => new GroupInfo
                {
                    Id = g.Id,
                    Name = g.Name,
                    UsersCount = g.Users.Count,
                    Role = g.Role.Name
                })
                .OrderBy(g => g.Name)
                .ToListAsync();

            return result;
        }

        public async Task<Dictionary<int, string>> GetGroupsName()
        {
            var result = await _context.Groups
                .Select(g => new { g.Id, g.Name })
                .OrderBy(g => g.Name)
                .ToDictionaryAsync(k => k.Id, v => v.Name);

            return result;
        }

        public async Task UpdateGroup(Group group)
        {
            _context.Groups.Update(group);
            await _context.SaveChangesAsync();
        }

        public async Task<string> GetGroupName(int groupId)
        {
            var result = await _context.Groups
                .Where(g => g.Id == groupId)
                .Select(g => g.Name)
                .FirstOrDefaultAsync();

            return result;
        }

        public async Task RemoveGroup(Group group)
        {
            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsGroupHasMonitors(string groupName)
        {
            var result = await _context.MonitorActions.AnyAsync(m => m.Group.Name == groupName);
            return result;
        }

        public async Task AddUserToGroup(int userId, int groupId)
        {
            var user = _context.Users.FirstOrDefault(x => x.Id == userId);
            if (user == null) { return; }

            var group = _context.Groups.FirstOrDefault(x => x.Id == groupId);
            if (group == null) { return; }

            group.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveUserFromGroup(int userId, int groupId)
        {
            var user = _context.Users.FirstOrDefault(x => x.Id == userId);
            if (user == null) { return; }

            var group = _context.Groups.FirstOrDefault(x => x.Id == groupId);
            if (group == null) { return; }

            group.Users.Remove(user);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsUserExists(int userId)
        {
            return await _context.Users.AnyAsync(u => u.Id == userId);
        }

        public async Task<bool> IsUserExistsInGroup(int userId, int groupId)
        {
            return await _context.Groups.AnyAsync(g => g.Id == groupId && g.Users.Any(u => u.Id == userId));
        }

        public async Task<bool> IsGroupExists(int groupId)
        {
            return await _context.Groups.AnyAsync(g => g.Id == groupId);
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