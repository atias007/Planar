using Dapper;
using Microsoft.EntityFrameworkCore;
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

        public async Task<List<string>> GetMonitorHooks()
        {
            return await _context.MonitorActions.Select(m => m.Hook).Distinct().ToListAsync();
        }

        public async Task<List<MonitorAction>> GetMonitorData(int @event, string groupName, string jobName)
        {
            var all = _context.MonitorActions
                .Include(m => m.Group)
                .ThenInclude(ug => ug.Users)
                .Where(m => m.EventId == @event && m.Active == true);

            var byJob = await all.Where(m => m.JobGroup == groupName && m.JobName == jobName).ToListAsync();
            var byGroup = await all.Where(m => m.JobGroup == groupName && string.IsNullOrEmpty(m.JobName)).ToListAsync();

            var final = byJob.Union(byGroup).Distinct().ToList();
            return final;
        }

        public async Task<MonitorAction> GetMonitorAction(int id)
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
                .Where(m => m.JobName == name && m.JobGroup == group)
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

        public async Task DeleteMonitorByJobId(string group, string name)
        {
            _context.MonitorActions.RemoveRange(e => e.JobGroup == group && e.JobName == name);
            await SaveChangesWithoutConcurrency();
        }

        public async Task DeleteMonitorByJobGroup(string jobGroup)
        {
            _context.MonitorActions.RemoveRange(e => e.JobGroup == jobGroup);
            await SaveChangesWithoutConcurrency();
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

        public async Task<bool> IsMonitorExists(int id)
        {
            return await _context.MonitorActions.AnyAsync(m => m.Id == id);
        }
    }
}