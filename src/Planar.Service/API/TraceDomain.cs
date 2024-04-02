using Planar.API.Common.Entities;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.API;

public class TraceDomain(IServiceProvider serviceProvider) : BaseLazyBL<TraceDomain, TraceData>(serviceProvider)
{
    public IQueryable<Model.Trace> GetTraceData()
    {
        return DataLayer.GetTraceData();
    }

    public IQueryable<Model.Trace> GetTrace(int key)
    {
        var trace = DataLayer.GetTrace(key);
        return trace;
    }

    public async Task<PagingResponse<LogDetails>> Get(GetTraceRequest request)
    {
        var result = await DataLayer.GetTrace(request);
        return result;
    }

    public async Task<string?> GetException(int id)
    {
        var result = await DataLayer.GetTraceException(id);

        if (result == null && !await DataLayer.IsTraceExists(id))
        {
            throw new RestNotFoundException($"trace with id {id} not found");
        }

        return result;
    }

    public async Task<string?> GetProperties(int id)
    {
        var result = await DataLayer.GetTraceProperties(id);

        if (result == null && !await DataLayer.IsTraceExists(id))
        {
            throw new RestNotFoundException($"trace with id {id} not found");
        }

        return result;
    }

    public async Task<CounterResponse> GetTraceCounter(CounterRequest request)
    {
        var result = new CounterResponse();
        var data = await DataLayer.GetTraceCounter(request);
        var list = new List<StatisticsCountItem>
        {
            new() { Label = nameof(data.Fatal), Count = data?.Fatal  ?? 0},
            new() { Label = nameof(data.Error), Count = data?.Error  ?? 0 },
            new() { Label = nameof(data.Warning), Count = data?.Warning ?? 0 },
            new() { Label = nameof(data.Information), Count = data?.Information ?? 0 },
            new() { Label = nameof(data.Debug), Count = data?.Debug ?? 0 }
        };

        result.Counter = list;
        return result;
    }
}