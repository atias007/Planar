using Planar.Client.Entities;
using RestSharp;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client.Api
{
    internal class ReportApi : BaseApi, IReportApi
    {
        public ReportApi(RestProxy proxy) : base(proxy)
        {
        }

        public async Task<IEnumerable<ReportsStatus>> GetAsync(ReportNames report, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("report/{name}", Method.Get)
                .AddUrlSegment("name", report);

            var result = await _proxy.InvokeAsync<IEnumerable<ReportsStatusInner>>(restRequest, cancellationToken);

            var final = result.Select(r => new ReportsStatus
            {
                Enabled = r.Enabled,
                Group = r.Group,
                NextRunning = r.NextRunning,
                Period = Enum.Parse<ReportPeriods>(r.Period, ignoreCase: true),
            });

            return final;
        }

        public async Task RunAsync(ReportNames report, ReportPeriods? period, string? group, CancellationToken cancellationToken = default)
        {
            var body = new
            {
                group,
                period = period?.ToString()
            };

            var restRequest = new RestRequest("report/{name}/run", Method.Post)
                .AddUrlSegment("name", report.ToString())
                .AddBody(body);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task RunAsync(ReportNames report, DateTime? fromDate, DateTime? toDate, string? group, CancellationToken cancellationToken = default)
        {
            var body = new
            {
                group,
                fromDate,
                toDate
            };

            var restRequest = new RestRequest("report/{name}/run", Method.Post)
                .AddUrlSegment("name", report.ToString())
                .AddBody(body);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task EnableAsync(ReportNames report, ReportPeriods period, string? group, CancellationToken cancellationToken = default)
        {
            var body = new
            {
                enable = true,
                group,
                period = period.ToString()
            };

            var restRequest = new RestRequest("report/{name}", Method.Patch)
                .AddUrlSegment("name", report.ToString())
                .AddBody(body);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task DisableAsync(ReportNames report, ReportPeriods period, CancellationToken cancellationToken = default)
        {
            var body = new
            {
                enable = false,
                period = period.ToString()
            };

            var restRequest = new RestRequest("report/{name}", Method.Patch)
                .AddUrlSegment("name", report.ToString())
                .AddBody(body);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task SetGroupAsync(ReportNames report, ReportPeriods period, string group, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(group, nameof(group));

            var body = new
            {
                group,
                period = period.ToString()
            };

            var restRequest = new RestRequest("report/{name}", Method.Patch)
                .AddUrlSegment("name", report.ToString())
                .AddBody(body);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task SetHourAsync(ReportNames report, ReportPeriods period, ushort hour, CancellationToken cancellationToken = default)
        {
            var body = new
            {
                HourOfDay = hour,
                period = period.ToString()
            };

            var restRequest = new RestRequest("report/{name}", Method.Patch)
                .AddUrlSegment("name", report.ToString())
                .AddBody(body);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }
    }
}