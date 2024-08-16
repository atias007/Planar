using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.CliGeneral;
using Planar.CLI.Entities;
using Planar.CLI.Proxy;
using RestSharp;
using Spectre.Console;
using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.Actions;

[Module("history", "Actions to inspect history runs of actions")]
public class HistoryCliActions : BaseCliAction<HistoryCliActions>
{
    [Action("ls")]
    [Action("list")]
    public static async Task<CliActionResponse> GetHistory(CliGetHistoryRequest request, CancellationToken cancellationToken = default)
    {
        var restRequest = new RestRequest("history", Method.Get);

        if (request.FromDate > DateTime.MinValue)
        {
            restRequest.AddQueryParameter("fromDate", request.FromDate.ToString("u"));
        }

        if (request.ToDate > DateTime.MinValue)
        {
            restRequest.AddQueryParameter("toDate", request.ToDate.ToString("u"));
        }

        if (request.Status != null)
        {
            restRequest.AddQueryParameter("status", request.Status.ToString());
        }

        if (!string.IsNullOrEmpty(request.JobId))
        {
            restRequest.AddQueryParameter("jobid", request.JobId);
        }

        if (!string.IsNullOrEmpty(request.JobGroup))
        {
            restRequest.AddQueryParameter("jobgroup", request.JobGroup);
        }

        if (!string.IsNullOrEmpty(request.JobType))
        {
            restRequest.AddQueryParameter("jobtype", request.JobType);
        }

        if (request.Outlier.HasValue)
        {
            restRequest.AddQueryParameter("outlier", request.Outlier.Value);
        }

        if (request.HasWarnings.HasValue)
        {
            restRequest.AddQueryParameter("haswarnings", request.HasWarnings.Value);
        }

        restRequest.AddQueryParameter("ascending", request.Ascending);
        restRequest.AddQueryPagingParameter(request);

        var result = await RestProxy.Invoke<PagingResponse<JobInstanceLogRow>>(restRequest, cancellationToken);
        var table = CliTableExtensions.GetTable(result.Data);
        return new CliActionResponse(result, table);
    }

    [Action("get")]
    public static async Task<CliActionResponse> GetHistoryById(CliGetBySomeIdRequest request, CancellationToken cancellationToken = default)
    {
        FillRequiredString(request, nameof(request.Id));
        if (IsOnlyDigits(request.Id.ToString()))
        {
            return await GetHistoryById(request.Id, cancellationToken);
        }

        return await GetHistoryByInstanceId(request.Id, cancellationToken);
    }

    [Action("count")]
    public static async Task<CliActionResponse> GetHistoryCount(CliGetCountRequest request, CancellationToken cancellationToken = default)
    {
        FillDatesScope(request);

        var restRequest = new RestRequest("history/count", Method.Get)
            .AddQueryDateScope(request);

        var result = await RestProxy.Invoke<CounterResponse>(restRequest, cancellationToken);
        if (!result.IsSuccessful || result.Data == null)
        {
            return new CliActionResponse(result);
        }

        var counter = result.Data.Counter;
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey54 bold underline]history status count[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new BarChart()
            .Width(60)
            .AddItem(counter[0].Label, counter[0].Count, Color.Gold1)
            .AddItem(counter[1].Label, counter[1].Count, Color.Green)
            .AddItem(counter[2].Label, counter[2].Count, Color.Red1));

        return CliActionResponse.Empty;
    }

    [Action("data")]
    public static async Task<CliActionResponse> GetHistoryDataById(CliGetByLongIdRequest request, CancellationToken cancellationToken = default)
    {
        FillRequiredLong(request, nameof(request.Id));

        var restRequest = new RestRequest("history/{id}/data", Method.Get)
           .AddParameter("id", request.Id, ParameterType.UrlSegment);

        var result = await RestProxy.Invoke<string>(restRequest, cancellationToken);
        return new CliActionResponse(result, message: result.Data);
    }

    [Action("odata")]
    public static async Task<CliActionResponse> GetHistoryOData(CliHistoryODataRequest request, CancellationToken cancellationToken = default)
    {
        var restRequest = new RestRequest("odata/historydata", Method.Get);
        if (!string.IsNullOrEmpty(request.Filter))
        {
            restRequest.AddQueryParameter("$filter", request.Filter);
        }

        if (!string.IsNullOrEmpty(request.OrderBy))
        {
            restRequest.AddQueryParameter("$orderby", request.OrderBy);
        }

        if (!string.IsNullOrEmpty(request.Select))
        {
            restRequest.AddQueryParameter("$select", request.Select);
        }

        if (request.Top > 0)
        {
            restRequest.AddQueryParameter("$top", request.Top.GetValueOrDefault());
        }

        if (request.Skip > 0)
        {
            restRequest.AddQueryParameter("$skip", request.Skip.GetValueOrDefault());
        }

        if (request.Count.HasValue)
        {
            restRequest.AddQueryParameter("$count", request.Count.GetValueOrDefault().ToString().ToLower());
        }

        var result = await RestProxy.Invoke(restRequest, cancellationToken);
        if (!result.IsSuccessStatusCode || string.IsNullOrWhiteSpace(result.Content))
        {
            return new CliActionResponse(result);
        }

        var token = JToken.Parse(result.Content).SelectToken("$.value")?.ToString();
        if (string.IsNullOrWhiteSpace(token))
        {
            return new CliActionResponse(result);
        }

        dynamic? jsonObject = JsonConvert.DeserializeObject(token);
        if (jsonObject == null)
        {
            return new CliActionResponse(result);
        }

        try
        {
            DataTable dt = JsonConvert.DeserializeObject<DataTable>(Convert.ToString(jsonObject));
            var table = CliTableExtensions.GetTable(dt);
            return new CliActionResponse(result, table);
        }
        catch
        {
            return new CliActionResponse(result);
        }
    }

    [Action("ex")]
    public static async Task<CliActionResponse> GetHistoryExceptionById(CliGetByLongIdRequestWithOutput request, CancellationToken cancellationToken = default)
    {
        FillRequiredLong(request, nameof(request.Id));

        var restRequest = new RestRequest("history/{id}/exception", Method.Get)
           .AddParameter("id", request.Id, ParameterType.UrlSegment);

        var result = await RestProxy.Invoke<string>(restRequest, cancellationToken);
        return new CliActionResponse(result, message: result.Data);
    }

    [Action("log")]
    public static async Task<CliActionResponse> GetHistoryLogById(CliGetByLongIdRequestWithOutput request, CancellationToken cancellationToken = default)
    {
        FillRequiredLong(request, nameof(request.Id));

        var restRequest = new RestRequest("history/{id}/log", Method.Get)
           .AddParameter("id", request.Id, ParameterType.UrlSegment);

        var result = await RestProxy.Invoke<string>(restRequest, cancellationToken);
        var message = CliFormat.GetLogMarkup(result.Data);
        return new CliActionResponse(result, message: message, formattedMessage: true);
    }

    [Action("summary")]
    public static async Task<CliActionResponse> GetHistorySummary(CliGetHistorySummaryRequest request, CancellationToken cancellationToken = default)
    {
        FillDatesScope(request);

        var restRequest = new RestRequest("history/summary", Method.Get);
        if (request.FromDate > DateTime.MinValue)
        {
            restRequest.AddQueryParameter("fromDate", request.FromDate.ToString("u"));
        }

        if (request.ToDate > DateTime.MinValue)
        {
            restRequest.AddQueryParameter("toDate", request.ToDate.ToString("u"));
        }

        restRequest.AddQueryPagingParameter(request);
        var result = await RestProxy.Invoke<PagingResponse<HistorySummary>>(restRequest, cancellationToken);
        var table = CliTableExtensions.GetTable(result.Data);
        return new CliActionResponse(result, table);
    }

    [Action("last")]
    public static async Task<CliActionResponse> GetLastHistoryCallForJob(CliGetLastHistoryCallForJobRequest request, CancellationToken cancellationToken = default)
    {
        var restRequest = new RestRequest("history/last", Method.Get);
        if (request.LastDays > 0)
        {
            restRequest.AddQueryParameter("lastDays", request.LastDays);
        }

        if (!string.IsNullOrEmpty(request.JobType))
        {
            restRequest.AddQueryParameter("jobType", request.JobType);
        }

        if (!string.IsNullOrEmpty(request.JobId))
        {
            restRequest.AddQueryParameter("jobId", request.JobId);
        }

        if (!string.IsNullOrEmpty(request.JobGroup))
        {
            restRequest.AddQueryParameter("jobGroup", request.JobGroup);
        }

        restRequest.AddQueryPagingParameter(request);
        var result = await RestProxy.Invoke<PagingResponse<JobLastRun>>(restRequest, cancellationToken);
        var table = CliTableExtensions.GetTable(result.Data);
        return new CliActionResponse(result, table);
    }

    private static async Task<CliActionResponse> GetHistoryById(string id, CancellationToken cancellationToken = default)
    {
        var restRequest = new RestRequest("history/{id}", Method.Get)
           .AddParameter("id", id, ParameterType.UrlSegment);

        var result = await RestProxy.Invoke<JobHistory>(restRequest, cancellationToken);
        return new CliActionResponse(result, dumpObject: result.Data);
    }

    private static async Task<CliActionResponse> GetHistoryByInstanceId(string id, CancellationToken cancellationToken = default)
    {
        var restRequest = new RestRequest("history/by-instanceid/{instanceid}", Method.Get)
                .AddParameter("instanceid", id, ParameterType.UrlSegment);

        var result = await RestProxy.Invoke<JobHistory>(restRequest, cancellationToken);
        return new CliActionResponse(result, dumpObject: result.Data);
    }

    private static bool IsOnlyDigits(string value)
    {
        if (value == null) { return true; }
        const string pattern = "^[0-9]+$";
        var regex = new Regex(pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
        return regex.IsMatch(value);
    }
}