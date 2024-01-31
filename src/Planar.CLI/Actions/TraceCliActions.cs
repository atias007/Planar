using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using Planar.CLI.General;
using Planar.CLI.Proxy;
using RestSharp;
using Spectre.Console;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("trace", "Actions to list trace log info", Synonyms = "traces")]
    public class TraceCliActions : BaseCliAction<TraceCliActions>
    {
        [Action("ls")]
        [Action("list")]
        public static async Task<CliActionResponse> GetTrace(CliGetTraceRequest request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("trace", Method.Get)
                .AddParameter("Ascending", request.Ascending, ParameterType.QueryString);

            if (!string.IsNullOrEmpty(request.Level))
            {
                restRequest.AddParameter("Level", request.Level, ParameterType.QueryString);
            }

            if (request.FromDate != DateTime.MinValue)
            {
                restRequest.AddParameter("FromDate", request.FromDate, ParameterType.QueryString);
            }

            if (request.ToDate != DateTime.MinValue)
            {
                restRequest.AddParameter("ToDate", request.ToDate, ParameterType.QueryString);
            }

            restRequest.AddQueryPagingParameter(request);
            var result = await RestProxy.Invoke<PagingResponse<LogDetails>>(restRequest, cancellationToken);
            var table = CliTableExtensions.GetTable(result.Data);
            return new CliActionResponse(result, table);
        }

        [Action("exception")]
        public static async Task<CliActionResponse> GetTraceException(CliGetByIdRequestWithOutput request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("trace/{id}/exception", Method.Get)
                .AddParameter("id", request.Id, ParameterType.UrlSegment);
            return await ExecuteEntity<string>(restRequest, cancellationToken);
        }

        [Action("prop")]
        public static async Task<CliActionResponse> GetTraceProperties(CliGetByIdRequestWithOutput request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("trace/{id}/properties", Method.Get)
                .AddParameter("id", request.Id, ParameterType.UrlSegment);
            var result = await RestProxy.Invoke<string>(restRequest, cancellationToken);
            var data = Util.BeautifyJson(result.Data);
            return new CliActionResponse(result, message: data);
        }

        [Action("count")]
        public static async Task<CliActionResponse> GetTraceCount(CliGetCountRequest request, CancellationToken cancellationToken = default)
        {
            FillDatesScope(request);

            var restRequest = new RestRequest("trace/count", Method.Get)
                .AddQueryDateScope(request);

            var result = await RestProxy.Invoke<CounterResponse>(restRequest, cancellationToken);
            if (!result.IsSuccessful || result.Data == null)
            {
                return new CliActionResponse(result);
            }

            var counter = result.Data.Counter;

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[grey54 bold underline]trace level count[/]");
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