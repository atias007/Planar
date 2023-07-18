using Dapper;
using Microsoft.EntityFrameworkCore;
using Planar.API.Common.Entities;
using Planar.Service.Model;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.Data
{
    public class MonitorData : BaseDataLayer
    {
        public MonitorData(PlanarContext context) : base(context)
        {
        }

        public async Task AddMonitor(MonitorAction request)
        {
            _context.MonitorActions.Add(request);
            await _context.SaveChangesAsync();
        }

        public async Task<int> CountFailsInHourForJob(object parameters)
        {
            var cmd = new CommandDefinition(
                commandText: "dbo.CountFailsInHourForJob",
                commandType: CommandType.StoredProcedure,
                parameters: parameters);

            var data = await DbConnection.QuerySingleAsync<int>(cmd);
            return data;
        }

        public async Task<int> CountFailsInRowForJob(object parameters)
        {
            var cmd = new CommandDefinition(
                commandText: "dbo.CountFailsInRowForJob",
                commandType: CommandType.StoredProcedure,
                parameters: parameters);

            var data = await DbConnection.QuerySingleAsync<int>(cmd);
            return data;
        }

        public async Task DeleteMonitor(MonitorAction request)
        {
            _context.MonitorActions.Remove(request);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteMonitorByJobGroup(string jobGroup)
        {
            await _context.MonitorActions
                .Where(m => m.JobGroup == jobGroup)
                .ExecuteDeleteAsync();
        }

        public async Task DeleteMonitorByJobId(string group, string name)
        {
            await _context.MonitorActions
                .Where(m =>
                     m.JobGroup == group &&
                     m.JobName == name)
                .ExecuteDeleteAsync();
        }

        public async Task<MonitorAction?> GetMonitorAction(int id)
        {
            return await _context.MonitorActions
                .Where(m => m.Id == id)
                .FirstOrDefaultAsync();
        }

        public IQueryable<MonitorAction> GetMonitorActions()
        {
            return _context.MonitorActions
                .AsNoTracking()
                .Include(i => i.Group)
                .OrderByDescending(d => d.Active)
                .ThenBy(d => d.JobGroup)
                .ThenBy(d => d.JobName)
                .ThenBy(d => d.Title);
        }

        public async Task<List<MonitorAction>> GetMonitorActionsByGroup(string group)
        {
            var result = await _context.MonitorActions
                .Include(m => m.Group)
                .Where(m =>
                    m.JobGroup != null && m.JobGroup.ToLower() == group.ToLower() &&
                    m.JobName == null)
                .OrderByDescending(d => d.Active)
                .ThenBy(d => d.JobGroup)
                .ThenBy(d => d.JobName)
                .ThenBy(d => d.Title)
                .ToListAsync();

            return result;
        }

        public async Task<List<MonitorAction>> GetMonitorActionsByJob(string group, string name)
        {
            var result = await _context.MonitorActions
                .Include(m => m.Group)
                .Where(m =>
                    m.JobGroup == group &&
                    (string.IsNullOrEmpty(m.JobName) || m.JobName.ToLower() == name.ToLower()))
                .OrderByDescending(d => d.Active)
                .ThenBy(d => d.JobGroup)
                .ThenBy(d => d.JobName)
                .ThenBy(d => d.Title)
                .ToListAsync();

            return result;
        }

        public IQueryable<MonitorAlert?> GetMonitorAlert(int id)
        {
            return _context.MonitorAlerts
                .AsNoTracking()
                .Where(a => a.Id == id);
        }

        public async Task<int> GetMonitorCount()
        {
            return await _context.MonitorActions.AsNoTracking().CountAsync();
        }

        public async Task<List<MonitorAction>> GetMonitorDataByEvent(int @event)
        {
            var data = await GetMonitorData()
                .AsNoTracking()
                .Where(m =>
                    m.EventId == @event &&
                    string.IsNullOrEmpty(m.JobGroup) &&
                    string.IsNullOrEmpty(m.JobName))
                .ToListAsync();

            return data;
        }

        public async Task<List<MonitorAction>> GetMonitorDataByGroup(int @event, string jobGroup)
        {
            var data = await GetMonitorData()
                .AsNoTracking()
                .Where(m =>
                    m.EventId == @event &&
                    m.JobGroup != null && m.JobGroup.ToLower() == jobGroup.ToLower() &&
                    string.IsNullOrEmpty(m.JobName))
                .ToListAsync();

            return data;
        }

        public async Task<List<MonitorAction>> GetMonitorDataByJob(int @event, string jobGroup, string jobName)
        {
            var data = await GetMonitorData()
                .AsNoTracking()
                .Where(m =>
                    m.EventId == @event &&
                    m.JobGroup != null && m.JobGroup.ToLower() == jobGroup.ToLower() &&
                    m.JobName != null && m.JobName.ToLower() == jobName.ToLower())
                .ToListAsync();

            return data;
        }

        public IQueryable<MonitorAlert> GetMonitorAlerts(GetMonitorsAlertsRequest request)
        {
            var query = _context.MonitorAlerts.AsNoTracking();

            if (request.FromDate.HasValue)
            {
                query = query.Where(l => l.AlertDate >= request.FromDate);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(l => l.AlertDate < request.ToDate);
            }

            if (!string.IsNullOrWhiteSpace(request.EventTitle))
            {
                query = query.Where(l => l.EventTitle.ToLower() == request.EventTitle.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(request.GroupName))
            {
                query = query.Where(l => l.GroupName.ToLower() == request.GroupName.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(request.Hook))
            {
                query = query.Where(l => l.Hook.ToLower() == request.Hook.ToLower());
            }

            if (request.MonitorId.HasValue)
            {
                query = query.Where(l => l.MonitorId == request.MonitorId);
            }

            if (request.HasError.HasValue)
            {
                query = query.Where(l => l.HasError == request.HasError);
            }

            if (!string.IsNullOrEmpty(request.JobId))
            {
                var index = request.JobId.IndexOf(".");
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

            if (request.Ascending.GetValueOrDefault())
            {
                query = query.OrderBy(l => l.AlertDate);
            }
            else
            {
                query = query.OrderByDescending(l => l.AlertDate);
            }

            return query;
        }

        public async Task<List<string>> GetMonitorUsedHooks()
        {
            return await _context.MonitorActions.Select(m => m.Hook).Distinct().ToListAsync();
        }

        public async Task<bool> IsMonitorExists(MonitorAction monitor)
        {
            return await _context.MonitorActions.AnyAsync(m =>
                m.EventId == monitor.EventId &&
                m.JobName == monitor.JobName &&
                m.JobGroup == monitor.JobGroup &&
                m.GroupId == monitor.GroupId &&
                m.Hook == monitor.Hook);
        }

        public async Task<bool> IsMonitorExists(MonitorAction monitor, int currentUpdateId)
        {
            return await _context.MonitorActions.AnyAsync(m =>
                m.Id != currentUpdateId &&
                m.EventId == monitor.EventId &&
                m.JobName == monitor.JobName &&
                m.JobGroup == monitor.JobGroup &&
                m.GroupId == monitor.GroupId &&
                m.Hook == monitor.Hook);
        }

        public async Task<bool> IsMonitorExists(int id)
        {
            return await _context.MonitorActions.AnyAsync(m => m.Id == id);
        }

        public async Task UpdateMonitorAction(MonitorAction monitor)
        {
            _context.MonitorActions.Update(monitor);
            await _context.SaveChangesAsync();
        }

        private IQueryable<MonitorAction> GetMonitorData()
        {
            var query = _context.MonitorActions
                .Include(m => m.Group)
                .ThenInclude(ug => ug.Users)
                .Where(m => m.Active == true);

            return query;
        }
    }
}