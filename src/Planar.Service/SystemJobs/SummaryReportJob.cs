using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.API;
using Planar.Service.Data;
using Planar.Service.Model;
using Polly;
using Quartz;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs
{
    public sealed class SummaryReportJob : SystemJob, IJob
    {
        private readonly ILogger<StatisticsJob> _logger;
        private readonly IServiceScopeFactory _serviceScope;

        private record struct HistorySummaryCounters(int Total, int Success, int Fail, int Running, int Retries, int Concurrent);
        private record struct DateScope(DateTime From, DateTime To);

        public SummaryReportJob(IServiceScopeFactory serviceScope, ILogger<StatisticsJob> logger)
        {
            _logger = logger;
            _serviceScope = serviceScope;
        }

        public static async Task Schedule(IScheduler scheduler, CancellationToken stoppingToken = default)
        {
            const string description = "System job for generating and send summary report";
            var span = TimeSpan.FromHours(24);
            var start = DateTime.Now.Date.AddDays(1).AddSeconds(1);
            await Schedule<SummaryReportJob>(scheduler, description, span, start, stoppingToken);
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var dateScope = GetDateScope(context);
                var emailsTask = GetEmails(context);
                var summaryTask = GetSummaryData(dateScope);
                var concurrentTask = GetMaxConcurrentExecutionData(dateScope);
                var summaryCounters = GetSummaryCounter(await summaryTask, await concurrentTask);
                var pausedTask = GetPausedTask();
                var lastRunningTask = GetLastRunningTask();
                var summaryTable = GetSummaryTable(await summaryTask);
                var pausedTable = GetPausedTable(await pausedTask, await lastRunningTask);

                var main = GetResource("main");
                main = ReplacePlaceHolder(main, "ReportDate", dateScope.From.ToShortDateString());
                main = ReplacePlaceHolder(main, "RunningDate", $"{DateTime.Now.ToShortDateString()} {DateTime.Now:HH:mm:ss}");

                main = ReplacePlaceHolder(main, "CubeTotal", GetSummaryRowCounter(summaryCounters.Total));
                main = ReplacePlaceHolder(main, "CubeSuccess", GetSummaryRowCounter(summaryCounters.Success));
                main = ReplacePlaceHolder(main, "CubeFail", GetSummaryRowCounter(summaryCounters.Fail));
                main = ReplacePlaceHolder(main, "CubeRunning", GetSummaryRowCounter(summaryCounters.Running));
                main = ReplacePlaceHolder(main, "CubeReries", GetSummaryRowCounter(summaryCounters.Retries));
                main = ReplacePlaceHolder(main, "CubeConcurrent", GetSummaryRowCounter(summaryCounters.Concurrent));

                main = ReplacePlaceHolder(main, "SummaryTable", summaryTable);
                main = ReplacePlaceHolder(main, "PausedTable", pausedTable);

                File.WriteAllText(@"C:\temp\planar.html", main);
                _logger?.LogInformation("Summary report generated");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Fail to send summary report: {Message}", ex.Message);
            }
        }

        private static DateScope GetDateScope(IJobExecutionContext context)
        {
            const string dataKey = "report.date";
            var from = DateTime.Now.Date.AddDays(-1);

            if (context.MergedJobDataMap.ContainsKey(dataKey))
            {
                var dateObject = context.MergedJobDataMap.Get(dataKey);
                var dateString = Convert.ToString(dateObject);
                if (DateTime.TryParse(dateString, CultureInfo.CurrentCulture, DateTimeStyles.None, out var reportDate))
                {
                    from = reportDate.Date;
                }
            }

            var to = from.AddDays(1);
            return new DateScope(from, to);
        }

        private async Task<IEnumerable<string>> GetEmails(IJobExecutionContext context)
        {
            const string dataKey = "report.group";
            if (!context.MergedJobDataMap.ContainsKey(dataKey))
            {
                throw new InvalidOperationException($"job data key '{dataKey}' (name of distribution group) could not found");
            }

            using var scope = _serviceScope.CreateScope();
            var groupData = scope.ServiceProvider.GetRequiredService<GroupData>();
            var groupName = context.MergedJobDataMap.GetString(dataKey);
            if (string.IsNullOrEmpty(groupName))
            {
                throw new InvalidOperationException($"distribution group '{dataKey}' could not found");
            }

            var id = await groupData.GetGroupId(groupName);
            var group = await groupData.GetGroupWithUsers(id)
                ?? throw new InvalidOperationException($"distribution group '{dataKey}' could not found");

            if (!group.Users.Any())
            {
                throw new InvalidOperationException($"distribution group '{dataKey}' has no users");
            }

            var emails1 = group.Users.Select(x => x.EmailAddress1);
            var emails2 = group.Users.Select(x => x.EmailAddress2);
            var emails3 = group.Users.Select(x => x.EmailAddress3);
            var allEmails = emails1.Union(emails2).Union(emails3).Where(x => !string.IsNullOrEmpty(x)).Distinct();

            if (!allEmails.Any())
            {
                throw new InvalidOperationException($"distribution group '{dataKey}' has no users with email");
            }

            return allEmails;
        }

        private static string GetSummaryTable(IEnumerable<HistorySummary> data)
        {
            if (!data.Any())
            {
                return GetResource("empty_table");
            }

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

        private static string GetPausedTable(IEnumerable<JobBasicDetails> data, IEnumerable<JobLastRun> lastRuns)
        {
            if (!data.Any())
            {
                return GetResource("empty_table");
            }

            var dictionary = new SortedDictionary<long, string>();
            foreach (var item in data)
            {
                var rowTemplate = GetResource("paused_row");
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

            var table = GetResource("paused_table");
            table = ReplacePlaceHolder(table, "PausedRow", rows.ToString());
            return table;
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
            using var scope = _serviceScope.CreateScope();
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

        private async Task<IEnumerable<JobBasicDetails>> GetPausedTask()
        {
            using var scope = _serviceScope.CreateScope();
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
            using var scope = _serviceScope.CreateScope();
            var historyDomain = scope.ServiceProvider.GetRequiredService<HistoryDomain>();
            var request = new GetLastHistoryCallForJobRequest
            {
                PageNumber = 1,
                PageSize = 1000
            };

            var response = await historyDomain.GetLastHistoryCallForJob(request);
            return response.Data ?? new List<JobLastRun>();
        }

        private async Task<int> GetMaxConcurrentExecutionData(DateScope dateScope)
        {
            using var scope = _serviceScope.CreateScope();
            var metricsData = scope.ServiceProvider.GetRequiredService<MetricsData>();
            var request = new MaxConcurrentExecutionRequest
            {
                FromDate = dateScope.From,
                ToDate = dateScope.To,
            };

            var response = await metricsData.GetMaxConcurrentExecution(request);
            return response;
        }

        private static string GetResource(string name)
        {
            var resourceName = $"{nameof(Planar)}.{nameof(Service)}.HtmlTemplates.SummaryReport.{name}.html";
            var assembly = typeof(SummaryReportJob).Assembly ??
                throw new InvalidOperationException("Assembly is null");
            using var stream = assembly.GetManifestResourceStream(resourceName) ??
                throw new InvalidOperationException($"Resource '{resourceName}' not found");
            using StreamReader reader = new(stream);
            var result = reader.ReadToEnd();
            return result;
        }

        private static string ReplacePlaceHolder(string template, string placeHolder, string? value)
        {
            var find = $"<!-- {{{{{placeHolder}}}}} -->";
            return template.Replace(find, value);
        }
    }
}