using Planar.Client.Entities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client.Api
{
    internal class MonitorApi : BaseApi, IMonitorApi
    {
        public MonitorApi(RestProxy proxy) : base(proxy)
        {
        }

        public async Task<int> AddAsync(AddMonitorRequest request, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(request, nameof(request));

            var restRequestAdd = new RestRequest("monitor", Method.Post)
                .AddBody(request);
            var result = await _proxy.InvokeAsync<PlanarIntIdResponse>(restRequestAdd, cancellationToken);
            return result?.Id ?? 0;
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));

            var restRequest = new RestRequest("monitor/{id}", Method.Delete)
                .AddParameter("id", id, ParameterType.UrlSegment);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task<MonitorAlertDetails> GetAlertAsync(int alertId, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(alertId, nameof(alertId));

            var restRequest = new RestRequest("monitor/alert/{id}", Method.Get)
                .AddUrlSegment("id", alertId);

            var result = await _proxy.InvokeAsync<MonitorAlertDetails>(restRequest, cancellationToken);
            return result;
        }

        public async Task<MonitorDetails> GetAsync(int id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));

            var restRequest = new RestRequest("monitor/{id}", Method.Get)
                .AddParameter("id", id, ParameterType.UrlSegment);

            var result = await _proxy.InvokeAsync<MonitorDetails>(restRequest, cancellationToken);
            return result;
        }

        public async Task<PagingResponse<MonitorAlertDetails>> ListAlertsAsync(ListAlertsFilter filter, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(filter, nameof(filter));

            var restRequest = new RestRequest("monitor/alerts", Method.Get)
                .AddEntityToQueryParameter(filter);

            var result = await _proxy.InvokeAsync<PagingResponse<MonitorAlertDetails>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<PagingResponse<MonitorDetails>> ListAsync(
            int? pageNumber = null,
            int? pageSize = null,
            CancellationToken cancellationToken = default)
        {
            var filter = new Paging(pageNumber, pageSize);

            var restRequest = new RestRequest("monitor", Method.Get)
                    .AddQueryPagingParameter(filter);

            var result = await _proxy.InvokeAsync<PagingResponse<MonitorDetails>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<IEnumerable<MonitorDetails>> ListByGroupAsync(string group, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(group, nameof(group));

            var restRequest = new RestRequest("monitor/by-group/{group}", Method.Get)
                  .AddParameter("group", group, ParameterType.UrlSegment);

            var result = await _proxy.InvokeAsync<List<MonitorDetails>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<IEnumerable<MonitorDetails>> ListByJobAsync(string jobId, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(jobId, nameof(jobId));

            var restRequest = new RestRequest("monitor/by-job/{jobId}", Method.Get)
                  .AddParameter("jobId", jobId, ParameterType.UrlSegment);

            var result = await _proxy.InvokeAsync<List<MonitorDetails>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<IEnumerable<MonitorEvent>> ListEventsAsync(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("monitor/events", Method.Get);
            var result = await _proxy.InvokeAsync<List<MonitorEvent>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<IEnumerable<HookDetails>> ListHooksAsync(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("monitor/hooks", Method.Get);
            var result = await _proxy.InvokeAsync<List<HookDetails>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<IEnumerable<MuteDetails>> ListMutes(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("monitor/mutes", Method.Get);
            var result = await _proxy.InvokeAsync<List<MuteDetails>>(restRequest, cancellationToken);
            return result;
        }

        public async Task MuteAsync(string jobId, int monitorId, DateTime dueDate, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(jobId, nameof(jobId));
            ValidateMandatory(monitorId, nameof(monitorId));
            ValidateMandatory(dueDate, nameof(dueDate));

            var restRequest = new RestRequest("monitor/mute", Method.Patch)
                .AddBody(new
                {
                    jobId,
                    monitorId,
                    dueDate
                });

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task Unmute(string jobId, int monitorId, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(jobId, nameof(jobId));
            ValidateMandatory(monitorId, nameof(monitorId));

            var restRequest = new RestRequest("monitor/unmute", Method.Patch)
                .AddBody(new
                {
                    jobId,
                    monitorId
                });

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

#if NETSTANDARD2_0

        public async Task UpdateAsync(int id, string propertyName, string propertyValue, CancellationToken cancellationToken = default)
#else
        public async Task UpdateAsync(int id, string propertyName, string? propertyValue, CancellationToken cancellationToken = default)
#endif
        {
            ValidateMandatory(id, nameof(id));

            var restRequest = new RestRequest("monitor", Method.Patch)
                .AddBody(new
                {
                    id,
                    propertyName,
                    propertyValue
                });

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }
    }
}