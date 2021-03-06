using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Planar.API.Common.Entities;
using Planar.Service.Model;
using Planar.Service.Model.DataObjects;
using Quartz;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using DbJobInstanceLog = Planar.Service.Model.JobInstanceLog;

namespace Planar.Service.Data
{
    public class DataLayer
    {
        private readonly PlanarContext _context;

        public DataLayer(PlanarContext context)
        {
            _context = context ?? throw new NullReferenceException(nameof(context));
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
            var paramInstanceId = new SqlParameter("@InstanceId", log.InstanceId);
            var paramStatus = new SqlParameter("@Status", log.Status);
            var paramStatusTitle = new SqlParameter("@StatusTitle", GetNullableSqlParameter(log.StatusTitle));
            var paramEndDate = new SqlParameter("@EndDate", GetNullableSqlParameter(log.EndDate));
            var paramDuration = new SqlParameter("@Duration", GetNullableSqlParameter(log.Duration));
            var paramEffectedRows = new SqlParameter("@EffectedRows", GetNullableSqlParameter(log.EffectedRows));
            var paramInformation = new SqlParameter("@Information", GetNullableSqlParameter(log.Information));
            var paramException = new SqlParameter("@Exception", GetNullableSqlParameter(log.Exception));
            var paramIsStopped = new SqlParameter("@IsStopped", log.IsStopped);

            await _context.Database.ExecuteSqlRawAsync(
                $"dbo.UpdateJobInstanceLog @InstanceId, @Status, @StatusTitle, @EndDate, @Duration, @EffectedRows, @Information, @Exception, @IsStopped",
                paramInstanceId, paramStatus, paramStatusTitle, paramEndDate, paramDuration, paramEffectedRows, paramInformation, paramException, paramIsStopped);
        }

        private static object GetNullableSqlParameter(object value)
        {
            if (value == null) { return DBNull.Value; }
            else { return value; }
        }

        public async Task PersistJobInstanceInformation(DbJobInstanceLog log)
        {
            var paramInstanceId = new SqlParameter("@InstanceId", log.InstanceId);
            var paramInformation = new SqlParameter("@Information", GetNullableSqlParameter(log.Information));
            var paramException = new SqlParameter("@Exception", GetNullableSqlParameter(log.Exception));

            await _context.Database.ExecuteSqlRawAsync($"dbo.PersistJobInstanceLog @InstanceId, @Information, @Exception", paramInstanceId, paramInformation, paramException);
        }

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

        public async Task<GlobalParameter> GetGlobalParameter(string key)
        {
            var result = await _context.GlobalParameters.FindAsync(key);
            return result;
        }

        public async Task<bool> IsGlobalParameterExists(string key)
        {
            var result = await _context.GlobalParameters.AnyAsync(p => p.ParamKey == key);
            return result;
        }

        public async Task<IEnumerable<GlobalParameter>> GetAllGlobalParameter()
        {
            var result = await _context.GlobalParameters.OrderBy(p => p.ParamKey).ToListAsync();
            return result;
        }

        public async Task AddGlobalParameter(GlobalParameter data)
        {
            _context.GlobalParameters.Add(data);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateGlobalParameter(GlobalParameter data)
        {
            _context.GlobalParameters.Update(data);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveGlobalParameter(string key)
        {
            var data = new GlobalParameter { ParamKey = key };
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

        public async Task<List<JobInstanceLogRow>> GetLastHistoryCallForJob(object parameters)
        {
            using var conn = _context.Database.GetDbConnection();
            var cmd = new CommandDefinition(
                commandText: "dbo.GetLastHistoryCallForJob",
                commandType: CommandType.StoredProcedure,
                parameters: parameters);

            var data = await conn.QueryAsync<JobInstanceLogRow>(cmd);
            return data.ToList();
        }

        public async Task<List<JobInstanceLogRow>> GetHistory(GetHistoryRequest request)
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
            var final = query.Select(l => new JobInstanceLogRow
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

        public async Task<string> GetHistoryInformationById(int id)
        {
            var result = await _context.JobInstanceLogs
                .Where(l => l.Id == id)
                .Select(l => l.Information)
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

        public async Task<string> GetPassword(int id)
        {
            var result = await _context.Users
                .Where(u => u.Id == id)
                .Select(u => u.Password)
                .FirstOrDefaultAsync();

            return result;
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
            var result = await _context.Groups.FindAsync(id);
            return result;
        }

        public async Task<List<GroupInfo>> GetGroups()
        {
            var result = await _context.Groups
                .Include(g => g.Users)
                .Select(g => new GroupInfo
                {
                    Id = g.Id,
                    Name = g.Name,
                    UsersCount = g.Users.Count
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

        public async Task RemoveGroup(Group group)
        {
            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();
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

        public async Task DeleteMonitor(MonitorAction request)
        {
            _context.MonitorActions.Remove(request);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteMonitorByJobId(string jobId)
        {
            _context.MonitorActions.RemoveRange(e => e.JobId == jobId);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteMonitorByJobGroup(string jobGroup)
        {
            _context.MonitorActions.RemoveRange(e => e.JobGroup == jobGroup);
            await _context.SaveChangesAsync();
        }

        #region Cluster

        public async Task<ClusterServer> GetClusterInstanceExists(ClusterServer item)
        {
            return await _context.ClusterServers.FirstOrDefaultAsync(c =>
                c.Server == item.Server &&
                c.Port == item.Port &&
                c.InstanceId == item.InstanceId);
        }

        public async Task UpdateClusterInstance(ClusterServer item)
        {
            _context.ClusterServers.Update(item);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateClusterHealthCheckDate(ClusterServer item)
        {
            var cluster = await GetClusterInstanceExists(item);
            cluster.HealthCheckDate = DateTime.Now;
            await UpdateClusterInstance(cluster);
        }

        public async Task AddClusterServer(ClusterServer item)
        {
            await _context.ClusterServers.AddAsync(item);
            await _context.SaveChangesAsync();
        }

        public void RemoveClusterServer(ClusterServer item)
        {
            _context.ClusterServers.Remove(item);
            _context.SaveChanges();
        }

        #endregion Cluster
    }
}