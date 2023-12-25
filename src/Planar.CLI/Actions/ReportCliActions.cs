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
    [Module("report", "Actions to execute, schedule and configure reports", Synonyms = "reports")]
    public class ReportCliActions : BaseCliAction<ReportCliActions>
    {
        [Action("enable")]
        [NullRequest]
        public static async Task<CliActionResponse> Enable(CliEnableReport request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                request = new CliEnableReport();
                await GetCliEnableReport(request, cancellationToken);
            }

            var body = new UpdateReportRequest
            {
                Enable = true,
                Group = request.Group,
                Period = request.Period.ToString()
            };

            var restRequest = new RestRequest("report/{name}", Method.Patch)
                .AddUrlSegment("name", request.Report)
                .AddBody(body);

            return await Execute(restRequest, cancellationToken);
        }

        [Action("disable")]
        [NullRequest]
        public static async Task<CliActionResponse> Disable(CliDisableReport request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                request = new CliEnableReport();
                await GetCliDisableReport(request, cancellationToken);
            }

            var body = new UpdateReportRequest
            {
                Enable = false,
                Group = null,
                Period = request.Period.ToString()
            };

            var restRequest = new RestRequest("report/{name}", Method.Patch)
                .AddUrlSegment("name", request.Report)
                .AddBody(body);

            return await Execute(restRequest, cancellationToken);
        }

        [Action("group")]
        [NullRequest]
        public static async Task<CliActionResponse> Group(CliEnableReport request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                request = new CliEnableReport();
                await GetCliSetReportGroup(request, cancellationToken);
            }

            var body = new UpdateReportRequest
            {
                Enable = null,
                Group = request.Group,
                Period = request.Period.ToString()
            };

            var restRequest = new RestRequest("report/{name}", Method.Patch)
                .AddUrlSegment("name", request.Report)
                .AddBody(body);

            return await Execute(restRequest, cancellationToken);
        }

        [Action("run")]
        [NullRequest]
        public static async Task<CliActionResponse> Run(CliEnableReport request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                request = new CliEnableReport();
                await GetCliEnableReport(request, cancellationToken);
            }

            var body = new RunReportRequest
            {
                Group = request.Group,
                Period = request.Period.ToString()
            };

            var restRequest = new RestRequest("report/{name}/run", Method.Post)
                .AddUrlSegment("name", request.Report)
                .AddBody(body);

            return await Execute(restRequest, cancellationToken);
        }

        [Action("get")]
        [NullRequest]
        public static async Task<CliActionResponse> Get(CliReport request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                request = new CliReport();
                await GetCliReport(request, cancellationToken);
            }

            var restRequest = new RestRequest("report/{name}", Method.Get)
                .AddUrlSegment("name", request.Report);

            return await ExecuteTable<IEnumerable<ReportsStatus>>(restRequest, CliTableExtensions.GetTable, cancellationToken);
        }

        [Action("ls")]
        [Action("list")]
        public static async Task<CliActionResponse> GetAll(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("report", Method.Get);
            var result = await RestProxy.Invoke<List<string>>(restRequest, cancellationToken);
            return new CliActionResponse(result, dumpObject: result.Data);
        }

        [Action("periods")]
        public static async Task<CliActionResponse> Periods(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("report/periods", Method.Get);
            var result = await RestProxy.Invoke<List<string>>(restRequest, cancellationToken);
            return new CliActionResponse(result, dumpObject: result.Data);
        }

        private static async Task<CliPromptWrapper> GetCliEnableReport(CliEnableReport request, CancellationToken cancellationToken)
        {
            // report
            var p1 = await CliPromptUtil.Reports(cancellationToken);
            if (!p1.IsSuccessful) { return p1; }
            request.Report = p1.Value ?? string.Empty;

            // periods
            var p2 = PromptSelection<ReportPeriods>("period");
            request.Period = p2;

            // group
            var restRequest = new RestRequest("report/{name}", Method.Get)
                .AddUrlSegment("name", request.Report);
            var response = await RestProxy.Invoke<IEnumerable<ReportsStatus>>(restRequest, cancellationToken);
            if (!response.IsSuccessful) { return new CliPromptWrapper<IEnumerable<ReportsStatus>>(response); }

            var currentGroup = response.Data?
                .Where(r => string.Equals(r.Period, request.Period.ToString(), StringComparison.OrdinalIgnoreCase))
                .Select(r => r.Group)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(currentGroup))
            {
                // enable report while current group is null
                // fill group name
                var p3 = await CliPromptUtil.Groups(cancellationToken);
                if (!p3.IsSuccessful) { return p3; }
                request.Group = p3.Value ?? string.Empty;
            }

            return CliPromptWrapper.Success;
        }

        private static async Task<CliPromptWrapper> GetCliSetReportGroup(CliEnableReport request, CancellationToken cancellationToken)
        {
            // report
            var p1 = await CliPromptUtil.Reports(cancellationToken);
            if (!p1.IsSuccessful) { return p1; }
            request.Report = p1.Value ?? string.Empty;

            // periods
            var p2 = PromptSelection<ReportPeriods>("period");
            request.Period = p2;

            // group
            var p3 = await CliPromptUtil.Groups(cancellationToken);
            if (!p3.IsSuccessful) { return p3; }
            request.Group = p3.Value ?? string.Empty;

            return CliPromptWrapper.Success;
        }

        private static async Task<CliPromptWrapper> GetCliDisableReport(CliDisableReport request, CancellationToken cancellationToken)
        {
            // report
            var p1 = await CliPromptUtil.Reports(cancellationToken);
            if (!p1.IsSuccessful) { return p1; }
            request.Report = p1.Value ?? string.Empty;

            // periods
            var p2 = PromptSelection<ReportPeriods>("period");
            request.Period = p2;

            return CliPromptWrapper.Success;
        }

        private static async Task<CliPromptWrapper> GetCliReport(CliReport request, CancellationToken cancellationToken)
        {
            var p1 = await CliPromptUtil.Reports(cancellationToken);
            if (!p1.IsSuccessful) { return p1; }
            request.Report = p1.Value ?? string.Empty;
            return CliPromptWrapper.Success;
        }
    }
}