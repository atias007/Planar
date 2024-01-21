using Planar.Client.Entities;
using RestSharp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client
{
    internal class HistoryApi : BaseApi, IHistoryApi
    {
        public HistoryApi(RestProxy proxy) : base(proxy)
        {
        }

        public async Task<HistoryDetails> Get(long id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("history/{id}", Method.Get)
            .AddParameter("id", id, ParameterType.UrlSegment);

            var result = await _proxy.InvokeAsync<HistoryDetails>(restRequest, cancellationToken);
            return result;
        }

        public async Task<HistoryDetails> Get(string instanceId, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(instanceId, nameof(instanceId));
            var restRequest = new RestRequest("history/by-instanceid/{instanceid}", Method.Get)
                .AddParameter("instanceid", instanceId, ParameterType.UrlSegment);

            var result = await _proxy.InvokeAsync<HistoryDetails>(restRequest, cancellationToken);
            return result;
        }

        public Task<CounterResponse> GetCounter(CounterFilter filter, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<string> GetData(long id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));

            var restRequest = new RestRequest("history/{id}/data", Method.Get)
               .AddParameter("id", id, ParameterType.UrlSegment);

            var result = await _proxy.InvokeAsync<string>(restRequest, cancellationToken);
            return result;
        }

        public async Task<string> GetException(long id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));

            var restRequest = new RestRequest("history/{id}/exception", Method.Get)
               .AddParameter("id", id, ParameterType.UrlSegment);

            var result = await _proxy.InvokeAsync<string>(restRequest, cancellationToken);
            return result;
        }

        public Task<PagingResponse<HistoryDetails>> GetLast(LastFilter filter, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<string> GetLog(long id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));

            var restRequest = new RestRequest("history/{id}/log", Method.Get)
               .AddParameter("id", id, ParameterType.UrlSegment);

            var result = await _proxy.InvokeAsync<string>(restRequest, cancellationToken);
            return result;
        }

        public Task<PagingResponse<HistorySummary>> GetSummary(SummaryFilter filter, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<PagingResponse<HistoryBasicDetails>> List(HistoryFilter? filter = null, CancellationToken cancellationToken = default)
        {
            filter ??= new HistoryFilter();

            var restRequest = new RestRequest("history", Method.Get);
            restRequest.AddQueryDateScope(filter);

            if (filter.Status != null)
            {
                restRequest.AddQueryParameter("status", filter.Status.ToString());
            }

            if (!string.IsNullOrEmpty(filter.JobId))
            {
                restRequest.AddQueryParameter("jobid", filter.JobId);
            }

            if (!string.IsNullOrEmpty(filter.JobGroup))
            {
                restRequest.AddQueryParameter("jobgroup", filter.JobGroup);
            }

            if (!string.IsNullOrEmpty(filter.JobType))
            {
                restRequest.AddQueryParameter("jobtype", filter.JobType);
            }

            if (filter.Outlier.HasValue)
            {
                restRequest.AddQueryParameter("outlier", filter.Outlier.Value);
            }

            restRequest.AddQueryParameter("ascending", filter.Ascending);
            restRequest.AddQueryPagingParameter(filter);

            var result = await _proxy.InvokeAsync<PagingResponse<HistoryBasicDetails>>(restRequest, cancellationToken);

            return result;
        }
    }
}