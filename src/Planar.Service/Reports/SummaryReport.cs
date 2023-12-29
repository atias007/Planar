using Microsoft.Extensions.DependencyInjection;
using Planar.API.Common.Entities;
using Planar.Service.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planar.Service.Reports
{
    public sealed class SummaryReport : BaseReport
    {
        private record struct HistorySummaryCounters(int Total, int Success, int Fail, int Running, int Retries, int Concurrent);

        public override string ReportName => "Summary";

        public SummaryReport(IServiceScopeFactory serviceScope) : base(serviceScope)
        {
        }

        public override async Task<string> Generate(DateScope dateScope)
        {
            var summaryTask = GetSummaryData(dateScope);
            var concurrentTask = GetMaxConcurrentExecutionData(dateScope);
            var summaryCounters = GetSummaryCounter(await summaryTask, await concurrentTask);
            var summaryTable = GetSummaryTable(await summaryTask);

            var main = GetMainTemplate();

            main = ReplacePlaceHolder(main, "ReportPeriod", dateScope.Period);
            main = ReplacePlaceHolder(main, "ReportDate.From", dateScope.From.ToShortDateString());
            main = ReplacePlaceHolder(main, "ReportDate.To", dateScope.To.ToShortDateString());
            main = ReplacePlaceHolder(main, "RunningDate", $"{DateTime.Now.ToShortDateString()} {DateTime.Now:HH:mm:ss}");
            main = FillCubes(main, summaryCounters);
            main = ReplacePlaceHolder(main, "SummaryTable", summaryTable);

            return main;
        }

        private static string FillCubes(string html, HistorySummaryCounters summaryCounters)
        {
            html = ReplacePlaceHolder(html, "CubeTotal", GetSummaryRowCounter(summaryCounters.Total));
            html = ReplacePlaceHolder(html, "CubeSuccess", GetSummaryRowCounter(summaryCounters.Success));
            html = ReplacePlaceHolder(html, "CubeFail", GetSummaryRowCounter(summaryCounters.Fail));
            html = ReplacePlaceHolder(html, "CubeRunning", GetSummaryRowCounter(summaryCounters.Running));
            html = ReplacePlaceHolder(html, "CubeReries", GetSummaryRowCounter(summaryCounters.Retries));
            html = ReplacePlaceHolder(html, "CubeConcurrent", GetSummaryRowCounter(summaryCounters.Concurrent));
            return html;
        }

        private string GetSummaryTable(IEnumerable<HistorySummary> data)
        {
            if (!data.Any()) { return EmptyTableHtml; }

            var rows = new StringBuilder();

            foreach (var item in data)
            {
                var rowTemplate = GetResource("summary_row");
                rowTemplate = ReplacePlaceHolder(rowTemplate, "JobId", item.JobId.ToString());
                rowTemplate = ReplacePlaceHolder(rowTemplate, "JobKey", $"{item.JobGroup}.{item.JobName}");
                rowTemplate = ReplacePlaceHolder(rowTemplate, "JobType", item.JobType);
                rowTemplate = ReplacePlaceHolder(rowTemplate, "Total", GetSummaryRowCounter(item.Total));
                rowTemplate = ReplacePlaceHolder(rowTemplate, "Success", GetSummaryRowCounter(item.Success));
                rowTemplate = ReplacePlaceHolder(rowTemplate, "Fail", GetSummaryRowCounter(item.Fail));
                rowTemplate = ReplacePlaceHolder(rowTemplate, "Running", GetSummaryRowCounter(item.Running));
                rowTemplate = ReplacePlaceHolder(rowTemplate, "Retries", GetSummaryRowCounter(item.Retries));
                rowTemplate = SetSummaryRowColors(rowTemplate, item);
                rows.AppendLine(rowTemplate);
            }

            var table = GetResource("summary_table");
            table = ReplacePlaceHolder(table, "SummaryRows", rows.ToString());
            return table;
        }

        private static string GetSummaryRowCounter(int counter)
        {
            if (counter == 0) { return "-"; }
            return counter.ToString("N0");
        }

        private static string SetSummaryRowColors(string row, HistorySummary data)
        {
            const string transparent = "transparent";
            row = data.Total == 0 ? row.Replace("#000", transparent) : row.Replace("#000", "#bac8d3");
            row = data.Success == 0 ? row.Replace("#111", transparent) : row.Replace("#111", "#d5e8d4");
            row = data.Fail == 0 ? row.Replace("#222", transparent) : row.Replace("#222", "#f8cecc");
            row = data.Running == 0 ? row.Replace("#333", transparent) : row.Replace("#333", "#fff2cc");
            row = data.Retries == 0 ? row.Replace("#444", transparent) : row.Replace("#444", "#dae8fc");

            return row;
        }

        private static HistorySummaryCounters GetSummaryCounter(IEnumerable<HistorySummary> summaryData, int concurrentData)
        {
            var result = new HistorySummaryCounters
            {
                Fail = summaryData.Sum(x => x.Fail),
                Retries = summaryData.Sum(x => x.Retries),
                Running = summaryData.Sum(x => x.Running),
                Success = summaryData.Sum(x => x.Success),
                Total = summaryData.Sum(x => x.Total),
                Concurrent = concurrentData
            };

            return result;
        }

        private async Task<IEnumerable<HistorySummary>> GetSummaryData(DateScope dateScope)
        {
            using var scope = ServiceScope.CreateScope();
            var historyData = scope.ServiceProvider.GetRequiredService<HistoryData>();
            var request = new GetSummaryRequest
            {
                FromDate = dateScope.From,
                ToDate = dateScope.To,
                PageNumber = 1,
                PageSize = 1000
            };
            var response = await historyData.GetHistorySummary(request);
            return response.Data ?? new List<HistorySummary>();
        }

        private async Task<int> GetMaxConcurrentExecutionData(DateScope dateScope)
        {
            using var scope = ServiceScope.CreateScope();
            var metricsData = scope.ServiceProvider.GetRequiredService<MetricsData>();
            var request = new MaxConcurrentExecutionRequest
            {
                FromDate = dateScope.From,
                ToDate = dateScope.To,
            };

            var response = await metricsData.GetMaxConcurrentExecution(request);
            return response;
        }
    }
}