using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.Data;
using Planar.Service.Model.DataObjects;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planar.Service.Reports
{
    public sealed class TraceReport(IServiceScopeFactory serviceScope) : BaseReport(serviceScope)
    {
        public override string ReportName => "Trace";

        public override async Task<string> Generate(DateScope dateScope)
        {
            var traceTask = GetTrace(dateScope);
            var countersTask = GetTraceCounters(dateScope);
            var traceTable = GetTraceTable(await traceTask);

            var main = GetMainTemplate();
            main = ReplaceEnvironmentPlaceHolder(main);
            main = FillCubes(main, await countersTask);
            main = ReplacePlaceHolder(main, "ReportPeriod", dateScope.Period);
            main = ReplacePlaceHolder(main, "ReportDate.From", dateScope.From.ToShortDateString());
            main = ReplacePlaceHolder(main, "ReportDate.To", dateScope.To.ToShortDateString());
            main = ReplacePlaceHolder(main, "TraceTable", traceTable);

            return main;
        }

        private static string FillCubes(string html, TraceStatusDto counters)
        {
            html = ReplacePlaceHolder(html, "CubeDebug", GetCounterText(counters.Debug));
            html = ReplacePlaceHolder(html, "CubeInformation", GetCounterText(counters.Information));
            html = ReplacePlaceHolder(html, "CubeWarning", GetCounterText(counters.Warning));
            html = ReplacePlaceHolder(html, "CubeError", GetCounterText(counters.Error));
            html = ReplacePlaceHolder(html, "CubeFatal", GetCounterText(counters.Fatal));
            html = ReplacePlaceHolder(html, "CubeTrace", GetCounterText(counters.Trace));
            return html;
        }

        private string GetTraceTable(IEnumerable<LogDetails> data)
        {
            if (!data.Any()) { return EmptyTableHtml; }

            var sb = new StringBuilder();

            foreach (var item in data)
            {
                var rowTemplate = GetResource("trace_row");
                rowTemplate = ReplacePlaceHolder(rowTemplate, "Message", item.Message, encode: true);
                rowTemplate = ReplacePlaceHolder(rowTemplate, "Level", item.Level);
                rowTemplate = ReplacePlaceHolder(rowTemplate, "TimeStamp", $"{item.TimeStamp.ToShortDateString()} {item.TimeStamp:HH:mm:ss}");
                rowTemplate = SetTraceRowColors(rowTemplate, item);
                sb.AppendLine(rowTemplate);
            }

            var table = GetResource("trace_table");
            table = ReplacePlaceHolder(table, "TraceRow", sb.ToString());
            return table;
        }

        private static string SetTraceRowColors(string row, LogDetails data)
        {
            const string transparent = "transparent";
            string color = data.Level switch
            {
                nameof(LogLevel.Error) => "#f8cecc",
                nameof(LogLevel.Warning) => "#fff2cc",
                nameof(LogLevel.Critical) or "Fatal" => "#dae8fc",
                _ => transparent,
            };
            row = row.Replace("#000", color);

            return row;
        }

        private async Task<TraceStatusDto> GetTraceCounters(DateScope dateScope)
        {
            using var scope = ServiceScope.CreateScope();
            var traceData = scope.ServiceProvider.GetRequiredService<TraceData>();
            var counterRequest = new CounterRequest
            {
                FromDate = dateScope.From,
                ToDate = dateScope.To
            };
            return await traceData.GetTraceCounter(counterRequest) ?? new TraceStatusDto();
        }

        private async Task<IEnumerable<LogDetails>> GetTrace(DateScope dateScope)
        {
            using var scope = ServiceScope.CreateScope();
            var traceData = scope.ServiceProvider.GetRequiredService<TraceData>();
            var request = new GetTraceRequest
            {
                PageNumber = 1,
                PageSize = 1000,
                FromDate = dateScope.From,
                ToDate = dateScope.To,
                Ascending = true
            };

            var response = await traceData.GetTraceForReport(request);
            return response.Data ?? [];
        }
    }
}