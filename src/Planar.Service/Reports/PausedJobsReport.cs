using Microsoft.Extensions.DependencyInjection;
using Planar.API.Common.Entities;
using Planar.Service.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planar.Service.Reports
{
    public sealed class PausedJobsReport : BaseReport
    {
        public override string ReportName => "Paused Jobs";

        public PausedJobsReport(IServiceScopeFactory serviceScope) : base(serviceScope)
        {
        }

        public override async Task<string> Generate(DateScope dateScope)
        {
            var pausedTask = GetPausedTask();
            var lastRunningTask = GetLastRunningTask();
            var pausedTable = GetPausedTable(await pausedTask, await lastRunningTask);

            var main = GetMainTemplate();

            main = ReplacePlaceHolder(main, "ReportPeriod", dateScope.Period);
            main = ReplacePlaceHolder(main, "RunningDate", $"{DateTime.Now.ToShortDateString()} {DateTime.Now:HH:mm:ss}");
            main = ReplacePlaceHolder(main, "PausedTable", pausedTable);

            return main;
        }

        private string GetPausedTable(IEnumerable<JobBasicDetails> data, IEnumerable<JobLastRun> lastRuns)
        {
            if (!data.Any()) { return EmptyTableHtml; }

            var dictionary = new SortedDictionary<long, string>();
            foreach (var item in data)
            {
                var rowTemplate = GetResource("pause_row");
                rowTemplate = ReplacePlaceHolder(rowTemplate, "JobId", item.Id.ToString());
                rowTemplate = ReplacePlaceHolder(rowTemplate, "JobKey", $"{item.Group}.{item.Name}");
                rowTemplate = ReplacePlaceHolder(rowTemplate, "JobType", item.JobType);
                rowTemplate = ReplacePlaceHolder(rowTemplate, "JobDescription", item.Description);

                var lastRun = lastRuns.FirstOrDefault(x => x.JobId == item.Id);
                var lastDate = lastRun?.StartDate;
                var lastTitle = lastDate == null ? "Never" : $"{lastDate.Value.ToShortDateString()} {lastDate.Value:HH:mm:ss}";
                rowTemplate = ReplacePlaceHolder(rowTemplate, "LastRunning", lastTitle);
                dictionary.Add(lastRun?.StartDate.Ticks ?? 0, rowTemplate);
            }

            var rows = new StringBuilder();
            foreach (var item in dictionary.Reverse())
            {
                rows.AppendLine(item.Value);
            }

            var table = GetResource("pause_table");
            table = ReplacePlaceHolder(table, "PausedRow", rows.ToString());
            return table;
        }

        private async Task<IEnumerable<JobBasicDetails>> GetPausedTask()
        {
            using var scope = ServiceScope.CreateScope();
            var jobDomain = scope.ServiceProvider.GetRequiredService<JobDomain>();
            var request = new GetAllJobsRequest
            {
                PageNumber = 1,
                PageSize = 1000,
                Active = false
            };

            var response = await jobDomain.GetAll(request);
            return response.Data ?? new List<JobBasicDetails>();
        }

        private async Task<IEnumerable<JobLastRun>> GetLastRunningTask()
        {
            using var scope = ServiceScope.CreateScope();
            var historyDomain = scope.ServiceProvider.GetRequiredService<HistoryDomain>();
            var request = new GetLastHistoryCallForJobRequest
            {
                PageNumber = 1,
                PageSize = 1000
            };

            var response = await historyDomain.GetLastHistoryCallForJob(request);
            return response.Data ?? new List<JobLastRun>();
        }
    }
}