using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.CliGeneral;
using Planar.CLI.Entities;
using Planar.CLI.Proxy;
using RestSharp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("metrics", "get metrics data & graphs", Synonyms = "metric")]
    public class MetricsCliActions : BaseCliAction<MetricsCliActions>
    {
        [Action("rebuild")]
        public static async Task<CliActionResponse> Rebuild(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("metrics/rebuild", Method.Post);
            return await Execute(restRequest, cancellationToken);
        }

        [Action("job-counters")]
        public static async Task<CliActionResponse> JobCounters(CliJobCountersRequest request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("metrics/job-counters", Method.Get);
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
            FillDatesScope(request);

            var restRequest = new RestRequest("metrics/concurrent", Method.Get)
                .AddQueryDateScope(request)
                .AddQueryPagingParameter(request);

            var result = await RestProxy.Invoke<PagingResponse<ConcurrentExecutionModel>>(restRequest, cancellationToken);
            var table = CliTableExtensions.GetTable(result.Data);
            return new CliActionResponse(result, table);
        }

        [Action("max-concurrent")]
        public static async Task<CliActionResponse> GetConcurrent(CliDateScope request, CancellationToken cancellationToken = default)
        {
            FillDatesScope(request);
            var restRequest = new RestRequest("metrics/max-concurrent", Method.Get)
               .AddQueryDateScope(request);

            var result = await RestProxy.Invoke<MaxConcurrentExecution>(restRequest, cancellationToken);
            return new CliActionResponse(result, new CliDumpObject(result.Data));
        }
    }
}