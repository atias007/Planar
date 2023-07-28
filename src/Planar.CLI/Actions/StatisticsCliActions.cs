using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.CliGeneral;
using Planar.CLI.Entities;
using Planar.CLI.Proxy;
using RestSharp;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("statistic", "Actions to get statistical data & graphs", Synonyms = "statistics")]
    public class StatisticsCliActions : BaseCliAction<StatisticsCliActions>
    {
        [Action("rebuild")]
        public static async Task<CliActionResponse> Rebuild(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("statistics/rebuild", Method.Post);
            return await Execute(restRequest, cancellationToken);
        }

        [Action("job-counters")]
        public static async Task<CliActionResponse> JobCounters(CliJobCountersRequest request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("statistics/job-counters", Method.Get);
            if (request.FromDate != default)
            {
                restRequest.AddQueryParameter("fromDate", request.FromDate.ToString("u"));
            }

            var result = await RestProxy.Invoke<JobCounters>(restRequest, cancellationToken);
            return new CliActionResponse(result, dumpObject: result?.Data);
        }

        [Action("concurrent")]
        public static async Task<CliActionResponse> GetConcurrent(CliGetConcurrentRequest request, CancellationToken cancellationToken = default)
        {
            request.Period ??= PromptSelection<ConcurrentPeriod>("select period of concurrent execution");
            if (request.Period == null) { return CliActionResponse.Empty; }
            DateTime fromDate = DateTime.Now;
            switch (request.Period!.Value)
            {
                case ConcurrentPeriod.Day:
                    fromDate = DateTime.Now.AddDays(-1);
                    break;

                case ConcurrentPeriod.Week:
                    fromDate = DateTime.Now.AddDays(-7);
                    break;

                case ConcurrentPeriod.Month:
                    fromDate = DateTime.Now.AddMonths(-1);
                    break;

                case ConcurrentPeriod.Year:
                    fromDate = DateTime.Now.AddYears(-1);
                    break;
            }

            var restRequest = new RestRequest("statistics/concurrent", Method.Get)
                .AddQueryParameter("fromDate", fromDate.ToString("u"));

            var result = await RestProxy.Invoke<List<ConcurrentExecutionModel>>(restRequest, cancellationToken);
            if (!result.IsSuccessful || result.Data == null) { return CliActionResponse.Empty; }

            var series = new List<double>();
            if (request.Period!.Value == ConcurrentPeriod.Day)
            {
                for (int i = 0; i < 24; i++)
                {
                    var value = result.Data.Where(d => d.RecordDate.Hour == i + 1).Select(d => d.MaxConcurrent).FirstOrDefault();
                    series.Add(value);
                    series.Add(value);
                    series.Add(value);
                }
            }
            else if (request.Period!.Value == ConcurrentPeriod.Week)
            {
                var start = fromDate;
                for (int i = 0; i < 84; i++)
                {
                    var values = result.Data
                        .Where(d => d.RecordDate >= start && d.RecordDate < start.AddHours(2))
                        .Select(d => d.MaxConcurrent);
                    var value = values.Any() ? values.Max() : 0;

                    series.Add(value);
                    start = start.AddHours(2);
                }
            }
            else if (request.Period!.Value == ConcurrentPeriod.Month)
            {
                var start = fromDate;
                var days = Convert.ToInt32(DateTime.Now.Subtract(fromDate).TotalDays);
                for (int i = 0; i < days * 2; i++)
                {
                    var values = result.Data
                        .Where(d => d.RecordDate >= start && d.RecordDate < start.AddHours(12))
                        .Select(d => d.MaxConcurrent);
                    var value = values.Any() ? values.Max() : 0;

                    series.Add(value);
                    start = start.AddHours(12);
                }
            }
            else if (request.Period!.Value == ConcurrentPeriod.Year)
            {
                var start = fromDate;
                for (int i = 0; i < 73; i++)
                {
                    var values = result.Data
                        .Where(d => d.RecordDate >= start && d.RecordDate < start.AddDays(5))
                        .Select(d => d.MaxConcurrent);
                    var value = values.Any() ? values.Max() : 0;

                    series.Add(value);
                    start = start.AddDays(5);
                }
            }

            return new CliActionResponse(result, new CliPlot(series));
        }
    }
}