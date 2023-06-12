using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using Planar.CLI.Proxy;
using RestSharp;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("history", "Actions to inspect history runs of actions")]
    public class HistoryCliActions : BaseCliAction<HistoryCliActions>
    {
        [Action("get")]
        public static async Task<CliActionResponse> GetHistoryById(CliGetByLongIdRequest request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("history/{id}", Method.Get)
               .AddParameter("id", request.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<CliJobInstanceLog>(restRequest, cancellationToken);
            return new CliActionResponse(result, dumpObject: result.Data);
        }

        [Action("ls")]
        [Action("list")]
        public static async Task<CliActionResponse> GetHistory(CliGetHistoryRequest request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("history", Method.Get);
            if (request.Rows > 0)
            {
                restRequest.AddQueryParameter("rows", request.Rows);
            }

            if (request.FromDate > DateTime.MinValue)
            {
                restRequest.AddQueryParameter("fromDate", request.FromDate);
            }

            if (request.ToDate > DateTime.MinValue)
            {
                restRequest.AddQueryParameter("toDate", request.ToDate);
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

            restRequest.AddQueryParameter("ascending", request.Ascending);

            var result = await RestProxy.Invoke<List<CliJobInstanceLog>>(restRequest, cancellationToken);
            var table = CliTableExtensions.GetTable(result.Data);
            return new CliActionResponse(result, table);
        }

        [Action("data")]
        public static async Task<CliActionResponse> GetHistoryDataById(CliGetByLongIdRequest request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("history/{id}/data", Method.Get)
               .AddParameter("id", request.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<string>(restRequest, cancellationToken);
            return new CliActionResponse(result, message: result.Data);
        }

        [Action("log")]
        public static async Task<CliActionResponse> GetHistoryLogById(CliGetByLongIdRequestWithOutput request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("history/{id}/log", Method.Get)
               .AddParameter("id", request.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<string>(restRequest, cancellationToken);
            return new CliActionResponse(result, message: result.Data);
        }

        [Action("ex")]
        public static async Task<CliActionResponse> GetHistoryExceptionById(CliGetByLongIdRequestWithOutput request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("history/{id}/exception", Method.Get)
               .AddParameter("id", request.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<string>(restRequest, cancellationToken);
            return new CliActionResponse(result, message: result.Data);
        }

        [Action("last")]
        public static async Task<CliActionResponse> GetLastHistoryCallForJob(CliGetLastHistoryCallForJobRequest request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("history/last", Method.Get)
                .AddQueryParameter("lastDays", request.LastDays);

            var result = await RestProxy.Invoke<List<CliJobInstanceLog>>(restRequest, cancellationToken);
            var table = CliTableExtensions.GetTable(result.Data);
            return new CliActionResponse(result, table);
        }

        [Action("count")]
        public static async Task<CliActionResponse> GetHistoryCount(CliGetCountRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Hours == 0)
            {
                request.Hours = GetCounterHours();
            }

            var restRequest = new RestRequest("history/count", Method.Get)
                .AddQueryParameter("hours", request.Hours);

            var result = await RestProxy.Invoke<CounterResponse>(restRequest, cancellationToken);
            if (!result.IsSuccessful || result.Data == null)
            {
                return new CliActionResponse(result);
            }

            var counter = result.Data.Counter;
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[grey54 bold underline]history status count for last {request.Hours} hours[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new BarChart()
                .Width(60)
                .AddItem(counter[0].Label, counter[0].Count, Color.Gold1)
                .AddItem(counter[1].Label, counter[1].Count, Color.Green)
                .AddItem(counter[2].Label, counter[2].Count, Color.Red1));

            return CliActionResponse.Empty;
        }
    }
}