using Planar.API.Common.Entities;
using Planar.Service.API.Helpers;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.MapperProfiles;
using Planar.Service.Model;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.API;

public class HistoryDomain(IServiceProvider serviceProvider) : BaseLazyBL<HistoryDomain, IHistoryData>(serviceProvider)
{
    #region OData

    public IQueryable<JobInstanceLog> GetHistoryData()
    {
        return DataLayer.GetHistoryData();
    }

    public IQueryable<JobInstanceLog> GetHistory(long key)
    {
        var history = DataLayer.GetHistory(key);
        return history;
    }

    #endregion OData

    public async Task<PagingResponse<JobInstanceLogRow>> GetHistory(GetHistoryRequest request)
    {
        var query = DataLayer.GetHistory(request);
        var data = await query.ProjectToWithPagingAsyc<JobInstanceLog, JobInstanceLogRow>(Mapper, request);
        var result = new PagingResponse<JobInstanceLogRow>(data);
        return result;
    }

    public async Task<PagingResponse<HistorySummary>> GetHistorySummary(GetSummaryRequest request)
    {
        request.SetPagingDefaults();
        var data = await DataLayer.GetHistorySummary(request);
        var items = data.Item1?.ToList() ?? [];
        var result = new PagingResponse<HistorySummary>(request, items, data.Item2);

        // fill author
        if (items.Count == 0) { return result; }

        var jobs = await Scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item.Author)) { continue; }
            var key = jobs.FirstOrDefault(j => j.Name == item.JobName && j.Group == item.JobGroup);
            if (key == null) { continue; }
            var job = await Scheduler.GetJobDetail(key);
            if (job == null) { continue; }
            item.Author = JobHelper.GetJobAuthor(job) ?? string.Empty;
        }

        return result;
    }

    public async Task<JobInstanceLog> GetHistoryById(long id)
    {
        var data = await DataLayer.GetHistoryById(id);
        var result = ValidateExistingEntity(data, "history");

        // === fix bug cause save \r\n in database ===
        result.Data = result.Data?.Trim();
        return result;
    }

    public async Task<int> GetHistoryStatusById(long id)
    {
        var data = await DataLayer.GetHistoryStatusById(id);
        return data == null ? throw new RestNotFoundException($"history id {id} could not be found") : data.GetValueOrDefault();
    }

    public async Task<JobInstanceLog> GetHistoryByInstanceId(string instanceid)
    {
        var data = await DataLayer.GetHistoryByInstanceId(instanceid);
        var result = ValidateExistingEntity(data, "history");

        // === fix bug cause save \r\n in database ===
        result.Data = result.Data?.Trim();
        return result;
    }

    public async Task<string?> GetHistoryDataById(long id)
    {
        var result = await DataLayer.GetHistoryDataById(id);
        await ValidateHistoryExists(id, result);
        return result;
    }

    public async Task<string?> GetHistoryLogById(long id)
    {
        var result = await DataLayer.GetHistoryLogById(id);

        // generate NotFound response
        await ValidateHistoryExists(id, result);
        return result;
    }

    public async Task<string?> GetHistoryExceptionById(long id)
    {
        var result = await DataLayer.GetHistoryExceptionById(id);

        // generate NotFound response
        await ValidateHistoryExists(id, result);

        return result;
    }

    public async Task<PagingResponse<JobLastRun>> GetLastHistoryCallForJob(GetLastHistoryCallForJobRequest request)
    {
        request.SetPagingDefaults();
        request.LastDays ??= 365;
        var last = await DataLayer.GetLastHistoryCallForJob(request);

        var mapper = new JobLastRunMapper();
        var data = last.Data?.Select(mapper.MapJobLastRun).ToList() ?? [];
        var result = new PagingResponse<JobLastRun>(request, data, last.TotalRows);
        return result;
    }

    private async Task ValidateHistoryExists(long id, string? result)
    {
        if (string.IsNullOrEmpty(result) && !await DataLayer.IsHistoryExists(id))
        {
            throw new RestNotFoundException($"history with id {id} not found");
        }
    }

    public async Task<CounterResponse> GetHistoryCounter(CounterRequest request)
    {
        var result = new CounterResponse();
        var data = await DataLayer.GetHistoryCounter(request);
        var list = new List<StatisticsCountItem>
        {
            new() { Label = nameof(data.Running), Count = data?.Running ?? 0 },
            new() { Label = nameof(data.Success), Count = data?.Success ?? 0},
            new() { Label = nameof(data.Fail), Count = data ?.Fail ?? 0 }
        };
        result.Counter = list;
        return result;
    }
}