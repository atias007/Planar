using Planar.Client.Entities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client.Api
{
    internal class MetricsApi : BaseApi, IMetricsApi
    {
        public MetricsApi(RestProxy proxy) : base(proxy)
        {
        }

        public async Task<MaxConcurrentExecution> GetMaxConcurrentExecutionAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
        {
            var dates = new DateScope(fromDate, toDate);
            var restRequest = new RestRequest("metrics/max-concurrent", Method.Get)
               .AddQueryDateScope(dates);

            var result = await _proxy.InvokeAsync<MaxConcurrentExecution>(restRequest, cancellationToken);
            return result;
        }

        public async Task<PagingResponse<ConcurrentExecutionDetails>> ListConcurrentExecutionAsync(ConcurrentFilter filter, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("metrics/concurrent", Method.Get)
                .AddQueryDateScope(filter)
                .AddQueryPagingParameter(filter);

            if (!string.IsNullOrWhiteSpace(filter.InstanceId))
            {
                restRequest.AddQueryParameter("instanceId", filter.InstanceId);
            }

            if (!string.IsNullOrWhiteSpace(filter.Server))
            {
                restRequest.AddQueryParameter("Server", filter.Server);
            }

            var result = await _proxy.InvokeAsync<PagingResponse<ConcurrentExecutionDetails>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<IEnumerable<JobMetrics>> ListMetricsAsync(string jobId, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(jobId, nameof(jobId));
            var restRequest = new RestRequest("metrics/job/{id}", Method.Get)
                .AddParameter("id", jobId, ParameterType.UrlSegment);

            var result = await _proxy.InvokeAsync<IEnumerable<JobMetrics>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<JobCounters> ListMetricsAsync(DateTime? fromDate = null, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("metrics/job-counters", Method.Get);
            if (fromDate != null)
            {
                restRequest.AddQueryParameter("fromDate", fromDate.Value.ToString("u"));
            }

            var result = await _proxy.InvokeAsync<JobCounters>(restRequest, cancellationToken);
            return result;
        }

        public async Task RebuildAsync(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("metrics/rebuild", Method.Post);
            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }
    }
}