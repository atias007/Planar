using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.Data;
using Quartz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs
{
    public sealed class SummaryReportJob : SystemJob, IJob
    {
        private readonly ILogger<StatisticsJob> _logger;
        private readonly IServiceScopeFactory _serviceScope;

        private record struct HistorySummaryCounters(int Total, int Success, int Fail, int Running, int Retries, int Concurrent);

        public SummaryReportJob(IServiceScopeFactory serviceScope, ILogger<StatisticsJob> logger)
        {
            _logger = logger;
            _serviceScope = serviceScope;
        }

        public static async Task Schedule(IScheduler scheduler, CancellationToken stoppingToken = default)
        {
            const string description = "System job for generating and send summary report";
            var span = TimeSpan.FromHours(24);
            var start = DateTime.Now.Date.AddDays(1).AddSeconds(5);
            await Schedule<SummaryReportJob>(scheduler, description, span, start, stoppingToken);
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var summaryTask = GetSummaryData();
                var concurrentTask = GetMaxConcurrentExecutionData();
                var summaryCounters = GetSummaryCounter(await summaryTask, await concurrentTask);

                var main = GetResource("main");
                main = ReplacePlaceHolder(main, "ReportDate", DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy"));
                main = ReplacePlaceHolder(main, "CubeTotal", summaryCounters.Total.ToString("N0"));
                main = ReplacePlaceHolder(main, "CubeSuccess", summaryCounters.Success.ToString("N0"));
                main = ReplacePlaceHolder(main, "CubeFail", summaryCounters.Fail.ToString("N0"));
                main = ReplacePlaceHolder(main, "CubeRunning", summaryCounters.Running.ToString("N0"));
                main = ReplacePlaceHolder(main, "CubeReries", summaryCounters.Retries.ToString("N0"));
                main = ReplacePlaceHolder(main, "CubeConcurrent", summaryCounters.Concurrent.ToString("N0"));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Fail to send summary report: {Message}", ex.Message);
            }
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

        private async Task<IEnumerable<HistorySummary>> GetSummaryData()
        {
            using var scope = _serviceScope.CreateScope();
            var historyData = scope.ServiceProvider.GetRequiredService<HistoryData>();
            var request = new GetSummaryRequest
            {
                FromDate = DateTime.Now.Date.AddDays(-1),
                ToDate = DateTime.Now.Date,
                PageNumber = 1,
                PageSize = 1000
            };
            var response = await historyData.GetHistorySummary(request);
            return response.Data ?? new List<HistorySummary>();
        }

        private async Task<int> GetMaxConcurrentExecutionData()
        {
            using var scope = _serviceScope.CreateScope();
            var metricsData = scope.ServiceProvider.GetRequiredService<MetricsData>();
            var request = new MaxConcurrentExecutionRequest
            {
                FromDate = DateTime.Now.Date.AddDays(-1),
                ToDate = DateTime.Now.Date,
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