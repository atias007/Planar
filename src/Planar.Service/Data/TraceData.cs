﻿using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.Model;
using Planar.Service.Model.DataObjects;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.Data;

public class TraceData(PlanarContext context) : BaseDataLayer(context)
{
    public async Task<int> ClearTraceTable(int overDays)
    {
        var parameters = new { OverDays = overDays };
        var cmd = new CommandDefinition(
            commandText: "dbo.ClearTrace",
            commandType: CommandType.StoredProcedure,
            parameters: parameters);

        return await DbConnection.ExecuteAsync(cmd);
    }

    public IQueryable<Trace> GetTrace(int key)
    {
        return _context.Traces.AsNoTracking().Where(t => t.Id == key);
    }

    public async Task<PagingResponse<LogDetails>> GetTrace(GetTraceRequest request)
    {
        var query = _context.Traces.AsNoTracking().AsQueryable();

        if (request.FromDate.HasValue)
        {
            query = query.Where(l => l.TimeStamp >= request.FromDate);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(l => l.TimeStamp < request.ToDate);
        }

        if (!string.IsNullOrEmpty(request.Level))
        {
            query = query.Where(l => l.Level == request.Level);
        }

        if (request.Ascending)
        {
            query = query.OrderBy(l => l.TimeStamp).ThenBy(l => l.Id);
        }
        else
        {
            query = query.OrderByDescending(l => l.TimeStamp).ThenBy(l => l.Id);
        }

        var final = query.Select(l => new LogDetails
        {
            Id = l.Id,
            Message = l.Message,
            Level = l.Level,
            TimeStamp = l.TimeStamp.ToLocalTime().DateTime
        });

        var result = await final.ToPagingListAsync(request);
        return result;
    }

    public async Task<PagingResponse<LogDetails>> GetTraceForReport(GetTraceRequest request)
    {
        var levels = new string[] { nameof(LogLevel.Critical), nameof(LogLevel.Warning), nameof(LogLevel.Error), "Fatal" };
        var query = _context.Traces
            .AsNoTracking()
            .AsQueryable()
            .Where(l => levels.Contains(l.Level));

        if (request.FromDate.HasValue)
        {
            query = query.Where(l => l.TimeStamp >= request.FromDate);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(l => l.TimeStamp < request.ToDate);
        }

        var final = query
            .OrderBy(l => l.TimeStamp)
            .ThenBy(l => l.Id)
            .Select(l => new LogDetails
            {
                Id = l.Id,
                Message = l.Message,
                Level = l.Level,
                TimeStamp = l.TimeStamp.ToLocalTime().DateTime
            });

        var result = await final.ToPagingListAsync(request);
        return result;
    }

    public IQueryable<Trace> GetTraceData()
    {
        return _context.Traces.AsNoTracking().OrderByDescending(t => t.TimeStamp).AsQueryable();
    }

    public async Task<string?> GetTraceException(int id)
    {
        var result = (await _context.Traces.FindAsync(id))?.Exception;
        return result;
    }

    public async Task<string?> GetTraceProperties(int id)
    {
        var result = (await _context.Traces.FindAsync(id))?.LogEvent;
        return result;
    }

    public async Task<bool> IsTraceExists(int id)
    {
        return await _context.Traces.AnyAsync(t => t.Id == id);
    }

    public async Task<TraceStatusDto?> GetTraceCounter(CounterRequest request)
    {
        var definition = new CommandDefinition(
            commandText: "[Statistics].[TraceCounter]",
            parameters: request,
            commandType: CommandType.StoredProcedure);

        var result = await DbConnection.QueryFirstOrDefaultAsync<TraceStatusDto>(definition);

        return result;
    }
}