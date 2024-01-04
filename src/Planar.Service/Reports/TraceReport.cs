using Microsoft.Extensions.DependencyInjection;
using Planar.API.Common.Entities;
using Planar.Service.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planar.Service.Reports
{
    public sealed class TraceReport : BaseReport
    {
        public override string ReportName => "Trace";

        public TraceReport(IServiceScopeFactory serviceScope) : base(serviceScope)
        {
        }

        public override async Task<string> Generate(DateScope dateScope)
        {
            var traceTask = GetTraceTask(dateScope);
            var traceTable = GetTraceTable(await traceTask);

            var main = GetMainTemplate();
            main = ReplacePlaceHolder(main, "ReportPeriod", dateScope.Period);
            main = ReplacePlaceHolder(main, "ReportDate.From", dateScope.From.ToShortDateString());
            main = ReplacePlaceHolder(main, "ReportDate.To", dateScope.To.ToShortDateString());
            main = ReplacePlaceHolder(main, "TraceTable", traceTable);

            return main;
        }

        private string GetTraceTable(IEnumerable<LogDetails> data)
        {
            if (!data.Any()) { return EmptyTableHtml; }

            var sb = new StringBuilder();

            foreach (var item in data)
            {
                var rowTemplate = GetResource("trace_row");
                rowTemplate = ReplacePlaceHolder(rowTemplate, "Message", item.Message);
                rowTemplate = ReplacePlaceHolder(rowTemplate, "Level", item.Level);
                rowTemplate = ReplacePlaceHolder(rowTemplate, "TimeStamp", $"{item.TimeStamp.ToShortDateString()} {item.TimeStamp:HH:mm:ss}");
                sb.AppendLine(rowTemplate);
            }

            var table = GetResource("pause_table");
            table = ReplacePlaceHolder(table, "PausedRow", sb.ToString());
            return table;
        }

        private async Task<IEnumerable<LogDetails>> GetTraceTask(DateScope dateScope)
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
            return response.Data ?? new List<LogDetails>();
        }
    }
}