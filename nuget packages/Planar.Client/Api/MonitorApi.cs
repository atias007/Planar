using Planar.Client.Entities;

using System;
using System.Collections.Generic;
using System.Net.Http;
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

            var restRequestAdd = new RestRequest("monitor", HttpMethod.Post)
                .AddBody(request);
            var result = await _proxy.InvokeAsync<PlanarIntIdResponse>(restRequestAdd, cancellationToken);
            return result?.Id ?? 0;
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));

            var restRequest = new RestRequest("monitor/{id}", HttpMethod.Delete)
                .AddSegmentParameter("id", id);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task<MonitorAlertDetails> GetAlertAsync(int alertId, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(alertId, nameof(alertId));

            var restRequest = new RestRequest("monitor/alert/{id}", HttpMethod.Get)
                .AddSegmentParameter("id", alertId);

            var result = await _proxy.InvokeAsync<MonitorAlertDetails>(restRequest, cancellationToken);
            return result;
        }

        public async Task<MonitorDetails> GetAsync(int id, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(id, nameof(id));

            var restRequest = new RestRequest("monitor/{id}", HttpMethod.Get)
                .AddSegmentParameter("id", id);

            var result = await _proxy.InvokeAsync<MonitorDetails>(restRequest, cancellationToken);
            return result;
        }

        public async Task<PagingResponse<MonitorAlertBasicDetails>> ListAlertsAsync(ListAlertsFilter filter, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(filter, nameof(filter));

            var restRequest = new RestRequest("monitor/alerts", HttpMethod.Get)
                .AddEntityToQueryParameter(filter);

            var result = await _proxy.InvokeAsync<PagingResponse<MonitorAlertBasicDetails>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<PagingResponse<MonitorDetails>> ListAsync(
            int? pageNumber = null,
            int? pageSize = null,
            CancellationToken cancellationToken = default)
        {
            var filter = new Paging(pageNumber, pageSize);

            var restRequest = new RestRequest("monitor", HttpMethod.Get)
                    .AddQueryPagingParameter(filter);

            var result = await _proxy.InvokeAsync<PagingResponse<MonitorDetails>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<IEnumerable<MonitorDetails>> ListByGroupAsync(string group, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(group, nameof(group));

            var restRequest = new RestRequest("monitor/by-group/{group}", HttpMethod.Get)
                  .AddSegmentParameter("group", group);

            var result = await _proxy.InvokeAsync<List<MonitorDetails>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<IEnumerable<MonitorDetails>> ListByJobAsync(string jobId, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(jobId, nameof(jobId));

            var restRequest = new RestRequest("monitor/by-job/{jobId}", HttpMethod.Get)
                  .AddSegmentParameter("jobId", jobId);

            var result = await _proxy.InvokeAsync<List<MonitorDetails>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<IEnumerable<MonitorEvent>> ListEventsAsync(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("monitor/events", HttpMethod.Get);
            var result = await _proxy.InvokeAsync<List<MonitorEvent>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<IEnumerable<HookDetails>> ListHooksAsync(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("monitor/hooks", HttpMethod.Get);
            var result = await _proxy.InvokeAsync<List<HookDetails>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<IEnumerable<MuteDetails>> ListMutes(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("monitor/mutes", HttpMethod.Get);
            var result = await _proxy.InvokeAsync<List<MuteDetails>>(restRequest, cancellationToken);
            return result;
        }

        public async Task MuteAsync(string jobId, int monitorId, DateTime dueDate, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(jobId, nameof(jobId));
            ValidateMandatory(monitorId, nameof(monitorId));
            ValidateMandatory(dueDate, nameof(dueDate));

            var restRequest = new RestRequest("monitor/mute", HttpPatchMethod)
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

            var restRequest = new RestRequest("monitor/unmute", HttpPatchMethod)
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

            var restRequest = new RestRequest("monitor", HttpPatchMethod)
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