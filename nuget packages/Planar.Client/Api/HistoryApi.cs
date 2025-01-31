﻿using Planar.Client.Entities;
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

        public async Task<HistoryDetails> GetAsync(long id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));
            var restRequest = new RestRequest("history/{id}", Method.Get)
            .AddParameter("id", id, ParameterType.UrlSegment);

            var result = await _proxy.InvokeAsync<HistoryDetails>(restRequest, cancellationToken);
            return result;
        }

        public async Task<HistoryDetails> GetAsync(string instanceId, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(instanceId, nameof(instanceId));
            var restRequest = new RestRequest("history/by-instanceid/{instanceid}", Method.Get)
                .AddParameter("instanceid", instanceId, ParameterType.UrlSegment);

            var result = await _proxy.InvokeAsync<HistoryDetails>(restRequest, cancellationToken);
            return result;
        }

        public async Task<CounterResponse> GetCounterAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
        {
            var filter = new CounterFilter(fromDate, toDate);

            var restRequest = new RestRequest("history/count", Method.Get)
                .AddQueryDateScope(filter);

            var result = await _proxy.InvokeAsync<CounterResponse>(restRequest, cancellationToken);
            return result;
        }

        public async Task<string> GetDataAsync(long id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));

            var restRequest = new RestRequest("history/{id}/data", Method.Get)
               .AddParameter("id", id, ParameterType.UrlSegment);

            var result = await _proxy.InvokeAsync<string>(restRequest, cancellationToken);
            return result;
        }

        public async Task<string> GetExceptionAsync(long id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));

            var restRequest = new RestRequest("history/{id}/exception", Method.Get)
               .AddParameter("id", id, ParameterType.UrlSegment);

            var result = await _proxy.InvokeAsync<string>(restRequest, cancellationToken);
            return result;
        }

#if NETSTANDARD2_0

        public async Task<PagingResponse<LastRunDetails>> LastAsync(LastHistoryFilter filter = null, CancellationToken cancellationToken = default)
        {
            if (filter == null) { filter = new LastHistoryFilter(); }
#else
        public async Task<PagingResponse<LastRunDetails>> LastAsync(LastHistoryFilter? filter = null, CancellationToken cancellationToken = default)
        {
            filter ??= new LastHistoryFilter();
#endif
            var restRequest = new RestRequest("history/last", Method.Get);
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

            if (filter.LastDays.GetValueOrDefault() > 0)
            {
                restRequest.AddQueryParameter("lastDays", filter.LastDays.GetValueOrDefault());
            }

            restRequest.AddQueryPagingParameter(filter);
            var result = await _proxy.InvokeAsync<PagingResponse<LastRunDetails>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<string> GetLogAsync(long id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));

            var restRequest = new RestRequest("history/{id}/log", Method.Get)
               .AddParameter("id", id, ParameterType.UrlSegment);

            var result = await _proxy.InvokeAsync<string>(restRequest, cancellationToken);
            return result;
        }

        public async Task<PagingResponse<HistorySummary>> SummaryAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? pageNumber = null,
            int? pageSize = null,
            CancellationToken cancellationToken = default)
        {
            var filter = new SummaryFilter
            {
                FromDate = fromDate,
                ToDate = toDate,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var restRequest = new RestRequest("history/summary", Method.Get)
              .AddQueryDateScope(filter)
              .AddQueryPagingParameter(filter);

            var result = await _proxy.InvokeAsync<PagingResponse<HistorySummary>>(restRequest, cancellationToken);
            return result;
        }

#if NETSTANDARD2_0

        public async Task<PagingResponse<HistoryBasicDetails>> ListAsync(ListHistoryFilter filter = null, CancellationToken cancellationToken = default)
        {
            if (filter == null) { filter = new ListHistoryFilter(); }
#else
        public async Task<PagingResponse<HistoryBasicDetails>> ListAsync(ListHistoryFilter? filter = null, CancellationToken cancellationToken = default)
        {
            filter ??= new ListHistoryFilter();
#endif

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

            if (filter.HasWarnings.HasValue)
            {
                restRequest.AddQueryParameter("haswarnings", filter.HasWarnings.Value);
            }

            restRequest.AddQueryParameter("ascending", filter.Ascending);
            restRequest.AddQueryPagingParameter(filter);

            var result = await _proxy.InvokeAsync<PagingResponse<HistoryBasicDetails>>(restRequest, cancellationToken);

            return result;
        }

#if NETSTANDARD2_0

        public async Task<string> ODataAsync(ODataFilter filter = null, CancellationToken cancellationToken = default)
        {
            if (filter == null) { filter = new ODataFilter(); }
#else
        public async Task<string> ODataAsync(ODataFilter? filter = null, CancellationToken cancellationToken = default)
        {
            filter ??= new ODataFilter();
#endif
            var restRequest = new RestRequest("odata/historydata", Method.Get);

            if (!string.IsNullOrWhiteSpace(filter.Filter))
            {
                restRequest.AddQueryParameter("$filter", filter.Filter);
            }

            if (!string.IsNullOrWhiteSpace(filter.Select))
            {
                restRequest.AddQueryParameter("$select", filter.Select);
            }

            if (!string.IsNullOrWhiteSpace(filter.OrderBy))
            {
                restRequest.AddQueryParameter("$orderby", filter.OrderBy);
            }

            if (filter.Top.HasValue)
            {
                restRequest.AddQueryParameter("$top", filter.Top.Value);
            }

            if (filter.Skip.HasValue)
            {
                restRequest.AddQueryParameter("$skip", filter.Skip.Value);
            }

            var result = await _proxy.InvokeAsync(restRequest, cancellationToken);
            return result ?? string.Empty;
        }
    }
}