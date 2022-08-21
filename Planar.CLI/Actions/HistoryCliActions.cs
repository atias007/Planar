using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("history")]
    public class HistoryCliActions : BaseCliAction<HistoryCliActions>
    {
        [Action("get")]
        public static async Task<CliActionResponse> GetHistoryById(CliGetByIdRequest request)
        {
            var restRequest = new RestRequest("history/{id}", Method.Get)
               .AddParameter("id", request.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<JobInstanceLog>(restRequest);
            return new CliActionResponse(result, serializeObj: result.Data);
        }

        [Action("ls")]
        [Action("list")]
        public static async Task<CliActionResponse> GetHistory(CliGetHistoryRequest request)
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

            if (string.IsNullOrEmpty(request.Status) == false)
            {
                restRequest.AddQueryParameter("status", request.Status);
            }

            restRequest.AddQueryParameter("ascending", request.Ascending);

            var result = await RestProxy.Invoke<List<JobInstanceLogRow>>(restRequest);
            var table = CliTableExtensions.GetTable(result.Data);
            return new CliActionResponse(result, table);
        }

        [Action("data")]
        public static async Task<CliActionResponse> GetHistoryDataById(CliGetByIdRequest request)
        {
            var restRequest = new RestRequest("history/{id}/data", Method.Get)
               .AddParameter("id", request.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<string>(restRequest);
            return new CliActionResponse(result, serializeObj: result.Data);
        }

        [Action("log")]
        public static async Task<CliActionResponse> GetHistoryLogById(CliGetByIdRequest request)
        {
            var restRequest = new RestRequest("history/{id}/log", Method.Get)
               .AddParameter("id", request.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<string>(restRequest);
            return new CliActionResponse(result, serializeObj: result.Data);
        }

        [Action("ex")]
        public static async Task<CliActionResponse> GetHistoryExceptionById(CliGetByIdRequest request)
        {
            var restRequest = new RestRequest("history/{id}/exception", Method.Get)
               .AddParameter("id", request.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<string>(restRequest);
            return new CliActionResponse(result, serializeObj: result.Data);
        }

        [Action("last")]
        public static async Task<CliActionResponse> GetLastHistoryCallForJob(CliGetLastHistoryCallForJobRequest request)
        {
            var restRequest = new RestRequest("history/last", Method.Get)
                .AddQueryParameter("lastDays", request.LastDays);

            var result = await RestProxy.Invoke<List<JobInstanceLogRow>>(restRequest);
            var table = CliTableExtensions.GetTable(result.Data);
            return new CliActionResponse(result, table);
        }
    }
}