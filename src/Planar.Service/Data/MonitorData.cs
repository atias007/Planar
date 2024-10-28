using Dapper;
using Microsoft.EntityFrameworkCore;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Common.Monitor;
using Planar.Service.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.Data;

public interface IMonitorData : IBaseDataLayer, IMonitorDurationDataLayer
{
    Task AddMonitor(MonitorAction request);

    Task AddMonitorCounter(MonitorCounter counter);

    Task AddMonitorHook(MonitorHook monitorHook);

    Task AddMonitorMute(MonitorMute request);

    Task<int> CountFailsInHourForJob(string jobId, int hours);

    Task<int> CountFailsInRowForJob(string jobId, int total);

    Task<int> DeleteMonitor(MonitorAction request);

    Task DeleteMonitorByJobGroup(string jobGroup);

    Task DeleteMonitorByJobId(string group, string name);

    Task DeleteMonitorCounterByJobId(string jobId);

    Task DeleteMonitorCounterByMonitorId(int monitorId);

    Task<int> DeleteMonitorHook(string name);

    Task DeleteOldMonitorMutes();

    Task<IEnumerable<MonitorHook>> GetAllMonitorHooks();

    Task<MonitorAction?> GetMonitorAction(int id);

    Task<IEnumerable<int>> GetMonitorActionIds();

    Task<IEnumerable<MonitorAction>> GetMonitorActions();

    Task<List<MonitorAction>> GetMonitorActionsByGroup(string group);

    Task<List<MonitorAction>> GetMonitorActionsByJob(string group, string name);

    IQueryable<MonitorAction> GetMonitorActionsQuery();

    IQueryable<MonitorAlert?> GetMonitorAlert(int id);

    IQueryable<MonitorAlert> GetMonitorAlerts(GetMonitorsAlertsRequest request);

    Task<int> GetMonitorCount();

    Task<int> GetMonitorCounter(string jobId, int monitorId, TimeSpan maxAlertsPeriod);

    Task<IEnumerable<int>> GetMonitorCounterIds();

    Task<IEnumerable<string>> GetMonitorCounterJobIds();

    Task<IEnumerable<MonitorCounter>> GetMonitorCounters(int limit);

    Task<int> GetMonitorEventId(int monitorId);

    Task<IEnumerable<MonitorMute>> GetMonitorMutes();

    Task<List<string>> GetMonitorUsedHooks();

    Task IncreaseMonitorCounter(string jobId, int monitorId);

    Task<bool> IsMonitorCounterExists(string jobId, int monitorId);

    Task<bool> IsMonitorExists(int id);

    Task<bool> IsMonitorExists(MonitorAction monitor);

    Task<bool> IsMonitorExists(MonitorAction monitor, int currentUpdateId);

    Task<bool> IsMonitorHookExists(string name);

    Task<bool> IsMonitorMuted(string jobId, int monitorId);

    Task ResetMonitorCounter(int delta);

    Task<int> SumEffectedRowsForJob(string jobId, DateTime since);

    Task UnMute();

    Task UnMute(int monitorId);

    Task UnMute(string jobId);

    Task UnMute(string jobId, int monitorId);

    Task UpdateMonitorAction(MonitorAction monitor);
}

public class MonitorDataSqlite(PlanarContext context) : MonitorData(context), IMonitorData
{
}

public class MonitorDataSqlServer(PlanarContext context) : MonitorData(context), IMonitorData
{
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1862:Use the 'StringComparison' method overloads to perform case-insensitive string comparisons", Justification = "EF Core")]
public class MonitorData(PlanarContext context) : BaseDataLayer(context)
{
    public async Task AddMonitor(MonitorAction request)
    {
        _context.MonitorActions.Add(request);
        await _context.SaveChangesAsync();
    }

    public async Task AddMonitorCounter(MonitorCounter counter)
    {
        _context.MonitorCounters.Add(counter);
        await _context.SaveChangesAsync();
    }

    public async Task AddMonitorHook(MonitorHook monitorHook)
    {
        _context.MonitorHooks.Add(monitorHook);
        await _context.SaveChangesAsync();
    }

    public async Task AddMonitorMute(MonitorMute request)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var tran = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted);
            await _context.MonitorMutes.Where(m => m.JobId == request.JobId && m.MonitorId == request.MonitorId).ExecuteDeleteAsync();
            _context.MonitorMutes.Add(request);
            await _context.SaveChangesAsync();
            await tran.CommitAsync();
        });
    }

    public async Task<int> CountFailsInHourForJob(string jobId, int hours)
    {
        var referenceDate = DateTime.Now.Date.AddHours(-hours);

        var result = await _context.JobInstanceLogs
            .Where(j => j.JobId == jobId && j.Status == 1 && j.EndDate > referenceDate)
            .CountAsync();
        return result;
    }

    public async Task<int> CountFailsInRowForJob(string jobId, int total)
    {
        var sum = await _context.JobInstanceLogs
            .Where(l => l.JobId == jobId)
            .OrderByDescending(l => l.Id)
            .Select(l => new { Status = l.Status == 1 ? 1 : 0 })
            .Take(total)
            .SumAsync(x => x.Status);

        return sum;
    }

    public async Task<int> DeleteMonitor(MonitorAction request)
    {
        var count = await _context.MonitorActions
            .Where(a => a.Id == request.Id)
            .ExecuteDeleteAsync();

        return count;
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

    public async Task DeleteMonitorCounterByJobId(string jobId)
    {
        await _context.MonitorCounters
            .Where(m => m.JobId == jobId)
            .ExecuteDeleteAsync();
    }

    public async Task DeleteMonitorCounterByMonitorId(int monitorId)
    {
        await _context.MonitorCounters
            .Where(m => m.MonitorId == monitorId)
            .ExecuteDeleteAsync();
    }

    public async Task<int> DeleteMonitorHook(string name)
    {
        var result = await _context.MonitorHooks
            .Where(m => m.Name == name)
            .ExecuteDeleteAsync();

        return result;
    }

    public async Task DeleteOldMonitorMutes()
    {
        await _context.MonitorMutes
           .Where(m => m.DueDate <= DateTime.Now)
           .ExecuteDeleteAsync();
    }

    public async Task<IEnumerable<MonitorHook>> GetAllMonitorHooks()
    {
        return await _context.MonitorHooks
            .OrderBy(h => h.Name)
            .ToListAsync();
    }

    public async Task<List<MonitorCacheItem>> GetDurationMonitorActions()
    {
        var result = await _context.MonitorActions
            .AsNoTracking()
            .Where(m =>
                m.EventId == (int)MonitorEvents.ExecutionDurationGreaterThanxMinutes &&
                m.Active
            )
            .Select(m => new MonitorCacheItem
            {
                EventArgument = m.EventArgument,
                JobGroup = m.JobGroup,
                JobName = m.JobName
            })
            .ToListAsync();

        return result;
    }

    public async Task<MonitorAction?> GetMonitorAction(int id)
    {
        return await _context.MonitorActions
            .AsNoTracking()
            .Where(m => m.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<int>> GetMonitorActionIds()
    {
        var count = await _context.MonitorActions
            .AsNoTracking()
            .Select(m => m.Id)
            .ToListAsync();

        return count;
    }

    public async Task<IEnumerable<MonitorAction>> GetMonitorActions()
    {
        return await _context.MonitorActions
            .AsSplitQuery()
            .Include(m => m.Group)
            .ThenInclude(m => m.Users)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<MonitorAction>> GetMonitorActionsByGroup(string group)
    {
        var result = await _context.MonitorActions
            .Include(m => m.Group)
            .Where(m =>
                m.JobGroup != null && m.JobGroup.ToLower() == group.ToLower())
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
            .AsNoTracking()
            .Where(m =>
                (m.JobGroup == null && m.JobName == null && m.EventId < 300) ||
                (m.JobGroup == group && (string.IsNullOrEmpty(m.JobName) || m.JobName.ToLower() == name.ToLower())))
            .OrderByDescending(d => d.Active)
            .ThenBy(d => d.JobGroup)
            .ThenBy(d => d.JobName)
            .ThenBy(d => d.Title)
            .ToListAsync();

        return result;
    }

    public IQueryable<MonitorAction> GetMonitorActionsQuery()
    {
        return _context.MonitorActions
            .AsNoTracking()
            .Include(i => i.Group)
            .OrderByDescending(d => d.Active)
            .ThenBy(d => d.JobGroup)
            .ThenBy(d => d.JobName)
            .ThenBy(d => d.Title);
    }

    public IQueryable<MonitorAlert?> GetMonitorAlert(int id)
    {
        return _context.MonitorAlerts
            .AsNoTracking()
            .Where(a => a.Id == id);
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
            query = query.Where(l => l.EventTitle != null && l.EventTitle.ToLower() == request.EventTitle.ToLower());
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

    public async Task<int> GetMonitorCount()
    {
        return await _context.MonitorActions.AsNoTracking().CountAsync();
    }

    public async Task<int> GetMonitorCounter(string jobId, int monitorId, TimeSpan maxAlertsPeriod)
    {
        var count = await _context.MonitorCounters
            .AsNoTracking()
            .Where(m =>
                m.JobId == jobId &&
                m.MonitorId == monitorId &&
                EF.Functions.DateDiffMinute(m.LastUpdate, DateTime.Now) <= maxAlertsPeriod.TotalMinutes)
            .Select(m => m.Counter)
            .FirstOrDefaultAsync();

        return count;
    }

    public async Task<IEnumerable<int>> GetMonitorCounterIds()
    {
        var result = await _context.MonitorCounters
            .AsNoTracking()
            .Select(m => m.MonitorId)
            .Distinct()
            .ToListAsync();

        return result;
    }

    public async Task<IEnumerable<string>> GetMonitorCounterJobIds()
    {
        var result = await _context.MonitorCounters
            .AsNoTracking()
            .Select(m => m.JobId)
            .Distinct()
            .ToListAsync();

        return result;
    }

    public async Task<IEnumerable<MonitorCounter>> GetMonitorCounters(int limit)
    {
        var outer = _context.MonitorCounters
            .AsNoTracking()
            .Where(c => c.Counter >= limit);

        var inner = _context.MonitorActions.AsNoTracking();

        return await outer.Join(inner,
                mutes => mutes.MonitorId,
                monitor => monitor.Id,
                (mute, monitor) => new MonitorCounter
                {
                    LastUpdate = mute.LastUpdate,
                    JobId = mute.JobId,
                    MonitorId = mute.MonitorId,
                    MonitorTitle = monitor.Title
                })

            .OrderBy(c => c.LastUpdate)
            .Take(1000)
            .ToListAsync();
    }

    public async Task<int> GetMonitorEventId(int monitorId)
    {
        return await _context.MonitorActions
            .AsNoTracking()
            .Where(m => m.Id == monitorId)
            .Select(m => m.EventId)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<MonitorMute>> GetMonitorMutes()
    {
        var outer = _context.MonitorMutes.AsNoTracking()
            .Where(m => m.DueDate > DateTime.Now);

        var inner = _context.MonitorActions.AsNoTracking();

        var query = from o in outer
                    join i in inner on o.MonitorId equals i.Id into joined
                    from j in joined.DefaultIfEmpty()
                    select new MonitorMute
                    {
                        DueDate = o.DueDate,
                        JobId = o.JobId,
                        MonitorId = o.MonitorId,
                        MonitorTitle = j.Title
                    };

        return await query
            .OrderBy(c => c.DueDate)
            .Take(1000)
            .ToListAsync();
    }

    public async Task<List<string>> GetMonitorUsedHooks()
    {
        return await _context.MonitorActions.Select(m => m.Hook).Distinct().ToListAsync();
    }

    public async Task IncreaseMonitorCounter(string jobId, int monitorId)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var tran = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await _context.MonitorCounters
                .Where(m => m.JobId == jobId && m.MonitorId == monitorId)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(p => p.Counter, p => p.Counter + 1)
                    .SetProperty(p => p.LastUpdate, DateTime.Now));
            await tran.CommitAsync();
        });
    }

    public async Task<bool> IsMonitorCounterExists(string jobId, int monitorId)
    {
        return await _context.MonitorCounters.AnyAsync(m => m.JobId == jobId && m.MonitorId == monitorId);
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

    public async Task<bool> IsMonitorHookExists(string name)
    {
        return await _context.MonitorHooks
            .AnyAsync(m => m.Name == name);
    }

    public async Task<bool> IsMonitorMuted(string jobId, int monitorId)
    {
        var result = await _context.MonitorMutes
            .AsNoTracking()
            .Where(m => m.DueDate >= DateTime.Now)
            .Where(m =>
                (m.JobId == null && m.MonitorId == null)
                ||
                (m.JobId == jobId && m.MonitorId == monitorId)
                ||
                (m.JobId == jobId && m.MonitorId == null)
                ||
                (m.JobId == null && m.MonitorId == monitorId))
            .AnyAsync();

        return result;
    }

    public async Task ResetMonitorCounter(int delta)
    {
        var referenceDate = DateTime.Now.AddMinutes(-delta);
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var tran = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await _context.MonitorCounters
                .Where(m => m.LastUpdate < referenceDate)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(p => p.Counter, 0)
                    .SetProperty(p => p.LastUpdate, DateTime.Now));
            await tran.CommitAsync();
        });
    }

    public async Task<int> SumEffectedRowsForJob(string jobId, DateTime since)
    {
        var result = await _context.JobInstanceLogs
            .AsNoTracking()
            .Where(l => l.JobId == jobId && l.StartDate >= since)
            .Select(l => l.EffectedRows.GetValueOrDefault())
            .SumAsync();

        return result;
    }

    public async Task UnMute(string jobId, int monitorId)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var tran = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted);
            await _context.MonitorMutes.Where(m => m.JobId == jobId && m.MonitorId == monitorId).ExecuteDeleteAsync();
            await _context.MonitorCounters.Where(m => m.JobId == jobId && m.MonitorId == monitorId).ExecuteDeleteAsync();
            await tran.CommitAsync();
        });
    }

    public async Task UnMute(string jobId)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var tran = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted);
            await _context.MonitorMutes.Where(m => m.JobId == jobId).ExecuteDeleteAsync();
            await _context.MonitorCounters.Where(m => m.JobId == jobId).ExecuteDeleteAsync();
            await tran.CommitAsync();
        });
    }

    public async Task UnMute(int monitorId)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var tran = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted);
            await _context.MonitorMutes.Where(m => m.MonitorId == monitorId).ExecuteDeleteAsync();
            await _context.MonitorCounters.Where(m => m.MonitorId == monitorId).ExecuteDeleteAsync();
            await tran.CommitAsync();
        });
    }

    public async Task UnMute()
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var tran = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted);
            await _context.MonitorMutes.ExecuteDeleteAsync();
            await _context.MonitorCounters.ExecuteDeleteAsync();
            await tran.CommitAsync();
        });
    }

    public async Task UpdateMonitorAction(MonitorAction monitor)
    {
        _context.MonitorActions.Update(monitor);
        await _context.SaveChangesAsync();
    }
}