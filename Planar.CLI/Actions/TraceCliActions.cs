using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("trace")]
    public class TraceCliActions : BaseCliAction<TraceCliActions>
    {
        [Action("ls")]
        [Action("list")]
        public static async Task<CliActionResponse> GetTrace(CliGetTraceRequest request)
        {
            var restRequest = new RestRequest("trace", Method.Get)
                .AddParameter("Rows", request.Rows == 0 ? null : request.Rows, ParameterType.QueryString)
                .AddParameter("Ascending", request.Ascending, ParameterType.QueryString)
                .AddParameter("FromDate", request.FromDate == DateTime.MinValue ? null : request.FromDate, ParameterType.QueryString)
                .AddParameter("ToDate", request.ToDate == DateTime.MinValue ? null : request.ToDate, ParameterType.QueryString)
                .AddParameter("Level", request.Level, ParameterType.QueryString);

            var result = await RestProxy.Invoke<List<LogDetails>>(restRequest);
            var table = CliTableExtensions.GetTable(result.Data);
            return new CliActionResponse(result, table);
        }

        [Action("ex")]
        public static async Task<CliActionResponse> GetTraceException(CliGetByIdRequest request)
        {
            var restRequest = new RestRequest("trace/{id}/exception", Method.Get)
                .AddParameter("id", request.Id, ParameterType.UrlSegment);
            var result = await RestProxy.Invoke<string>(restRequest);
            return new CliActionResponse(result, message: result.Data);
        }

        [Action("prop")]
        public static async Task<CliActionResponse> GetTraceProperties(CliGetByIdRequest request)
        {
            var restRequest = new RestRequest("trace/{id}/properties", Method.Get)
                .AddParameter("id", request.Id, ParameterType.UrlSegment);
            var result = await RestProxy.Invoke<string>(restRequest);
            return new CliActionResponse(result, message: result.Data);
        }
    }
}