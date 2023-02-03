using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using Planar.CLI.General;
using RestSharp;
using Spectre.Console;
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
                .AddParameter("Ascending", request.Ascending, ParameterType.QueryString)
                .AddParameter("Level", request.Level, ParameterType.QueryString);

            if (request.Rows > 0)
            {
                restRequest.AddParameter("Rows", request.Rows, ParameterType.QueryString);
            }

            if (request.FromDate != DateTime.MinValue)
            {
                restRequest.AddParameter("FromDate", request.FromDate, ParameterType.QueryString);
            }

            if (request.ToDate != DateTime.MinValue)
            {
                restRequest.AddParameter("ToDate", request.ToDate, ParameterType.QueryString);
            }

            var result = await RestProxy.Invoke<List<LogDetails>>(restRequest);
            var table = CliTableExtensions.GetTable(result.Data);
            return new CliActionResponse(result, table);
        }

        [Action("ex")]
        public static async Task<CliActionResponse> GetTraceException(CliGetByIdRequest request)
        {
            var restRequest = new RestRequest("trace/{id}/exception", Method.Get)
                .AddParameter("id", request.Id, ParameterType.UrlSegment);
            return await ExecuteEntity<string>(restRequest);
        }

        [Action("prop")]
        public static async Task<CliActionResponse> GetTraceProperties(CliGetByIdRequest request)
        {
            var restRequest = new RestRequest("trace/{id}/properties", Method.Get)
                .AddParameter("id", request.Id, ParameterType.UrlSegment);
            var result = await RestProxy.Invoke<string>(restRequest);
            var data = Util.BeautifyJson(result.Data);
            return new CliActionResponse(result, message: data);
        }

        [Action("count")]
        public static async Task<CliActionResponse> GetTraceCount(CliGetCountRequest request)
        {
            var restRequest = new RestRequest("trace/count", Method.Get)
                .AddQueryParameter("hours", request.Hours);

            var result = await RestProxy.Invoke<CounterResponse>(restRequest);
            if (!result.IsSuccessful || result.Data == null)
            {
                return new CliActionResponse(result);
            }

            var counter = result.Data.Counter;

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[grey54 bold underline]trace level count for last {request.Hours} hours[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new BreakdownChart()
                .Width(60)
                .AddItem(counter[0].Label, counter[0].Count, Color.HotPink)
                .AddItem(counter[1].Label, counter[1].Count, Color.Red1)
                .AddItem(counter[2].Label, counter[2].Count, Color.Gold1)
                .AddItem(counter[3].Label, counter[3].Count, Color.SteelBlue1)
                .AddItem(counter[4].Label, counter[4].Count, Color.DarkOliveGreen3_2));

            return CliActionResponse.Empty;
        }
    }
}