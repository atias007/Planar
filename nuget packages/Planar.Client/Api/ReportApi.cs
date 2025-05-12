using Planar.Client.Entities;

using System;
using System.Collections.Generic;
using System.Net.Http;
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
            var restRequest = new RestRequest("report/{name}", HttpMethod.Get)
                .AddSegmentParameter("name", report);

            var result = await _proxy.InvokeAsync<IEnumerable<ReportsStatus>>(restRequest, cancellationToken);
            return result;
        }

#if NETSTANDARD2_0

        public async Task RunAsync(ReportNames report, ReportPeriods? period, string group, CancellationToken cancellationToken = default)
#else
        public async Task RunAsync(ReportNames report, ReportPeriods? period, string? group, CancellationToken cancellationToken = default)
#endif
        {
            var body = new
            {
                group,
                period = period?.ToString()
            };

            var restRequest = new RestRequest("report/{name}/run", HttpMethod.Post)
                .AddSegmentParameter("name", report.ToString())
                .AddBody(body);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

#if NETSTANDARD2_0

        public async Task RunAsync(ReportNames report, DateTime? fromDate, DateTime? toDate, string group, CancellationToken cancellationToken = default)
#else
        public async Task RunAsync(ReportNames report, DateTime? fromDate, DateTime? toDate, string? group, CancellationToken cancellationToken = default)
#endif
        {
            var body = new
            {
                group,
                fromDate,
                toDate
            };

            var restRequest = new RestRequest("report/{name}/run", HttpMethod.Post)
                .AddSegmentParameter("name", report.ToString())
                .AddBody(body);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

#if NETSTANDARD2_0

        public async Task EnableAsync(ReportNames report, ReportPeriods period, string group, CancellationToken cancellationToken = default)
#else
        public async Task EnableAsync(ReportNames report, ReportPeriods period, string? group, CancellationToken cancellationToken = default)
#endif
        {
            var body = new
            {
                enable = true,
                group,
                period = period.ToString()
            };

            var restRequest = new RestRequest("report/{name}", HttpPatchMethod)
                .AddSegmentParameter("name", report.ToString())
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

            var restRequest = new RestRequest("report/{name}", HttpPatchMethod)
                .AddSegmentParameter("name", report.ToString())
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

            var restRequest = new RestRequest("report/{name}", HttpPatchMethod)
                .AddSegmentParameter("name", report.ToString())
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

            var restRequest = new RestRequest("report/{name}", HttpPatchMethod)
                .AddSegmentParameter("name", report.ToString())
                .AddBody(body);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }
    }
}