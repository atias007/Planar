using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.Model;
using Quartz;
using System;
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

        public async Task<int> GetMonitorCount()
        {
            return await _context.MonitorActions.CountAsync();
        }

        public async Task<List<string>> GetMonitorUsedHooks()
        {
            return await _context.MonitorActions.Select(m => m.Hook).Distinct().ToListAsync();
        }

        public async Task<List<MonitorAction>> GetMonitorDataByEvent(int @event)
        {
            var data = await GetMonitorData()
                .Where(m => m.EventId == @event && string.IsNullOrEmpty(m.JobGroup) && string.IsNullOrEmpty(m.JobName))
                .ToListAsync();

            return data;
        }

        public async Task<List<MonitorAction>> GetMonitorDataByGroup(int @event, string jobGroup)
        {
            var data = await GetMonitorData()
                .Where(m => m.EventId == @event && m.JobGroup == jobGroup && string.IsNullOrEmpty(m.JobName))
                .ToListAsync();

            return data;
        }

        public async Task<List<MonitorAction>> GetMonitorDataByJob(int @event, string jobGroup, string jobName)
        {
            var data = await GetMonitorData()
                .Where(m => m.EventId == @event && m.JobGroup == jobGroup && m.JobName == jobName)
                .ToListAsync();

            return data;
        }

        private IQueryable<MonitorAction> GetMonitorData()
        {
            var query = _context.MonitorActions
                .Include(m => m.Group)
                .ThenInclude(ug => ug.Users)
                .Where(m => m.Active == true);

            return query;
        }

        public async Task<MonitorAction?> GetMonitorAction(int id)
        {
            return await _context.MonitorActions
                .AsNoTracking()
                .Where(m => m.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<List<MonitorAction>> GetMonitorActionsByJob(string group, string name)
        {
            var result = await _context.MonitorActions
                .Include(m => m.Group)
                .Where(m => m.JobGroup == group && (m.JobName == name || string.IsNullOrEmpty(m.JobName)))
                .OrderByDescending(m => m.Active)
                .ThenBy(m => m.JobGroup)
                .ThenBy(m => m.JobName)
                .ToListAsync();

            return result;
        }

        public async Task<List<MonitorAction>> GetMonitorActionsByGroup(string group)
        {
            var result = await _context.MonitorActions
                .Include(m => m.Group)
                .Where(m => m.JobGroup == group && m.JobName == null)
                .OrderByDescending(m => m.Active)
                .ThenBy(m => m.JobGroup)
                .ThenBy(m => m.JobName)
                .ToListAsync();

            return result;
        }

        public async Task<List<MonitorAction>> GetMonitorActions()
        {
            return await _context.MonitorActions
                .Include(i => i.Group)
                .OrderByDescending(d => d.Active)
                .ThenBy(d => d.JobGroup)
                .ThenBy(d => d.JobName)
                .ThenBy(d => d.Title)
                .ToListAsync();
        }

        public async Task AddMonitor(MonitorAction request)
        {
            _context.MonitorActions.Add(request);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteMonitor(MonitorAction request)
        {
            _context.MonitorActions.Remove(request);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteMonitorByJobId(string group, string name)
        {
            await _context.MonitorActions.Where(e => e.JobGroup == group && e.JobName == name).ExecuteDeleteAsync();
        }

        public async Task DeleteMonitorByJobGroup(string jobGroup)
        {
            await _context.MonitorActions.Where(e => e.JobGroup == jobGroup).ExecuteDeleteAsync();
        }

        public async Task UpdateMonitorAction(MonitorAction monitor)
        {
            _context.MonitorActions.Update(monitor);
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

        public async Task<bool> IsMonitorExists(MonitorAction monitor)
        {
            return await _context.MonitorActions.AnyAsync(m =>
                m.EventId == monitor.EventId &&
                m.EventArgument == monitor.EventArgument &&
                m.JobName == monitor.JobName &&
                m.JobGroup == monitor.JobGroup &&
                m.GroupId == monitor.GroupId &&
                m.Hook == monitor.Hook);
        }

        public async Task<bool> IsMonitorExists(int id)
        {
            return await _context.MonitorActions.AnyAsync(m => m.Id == id);
        }
    }
}