using Planar.Client.Entities;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client.Api
{
    internal class ServiceApi : BaseApi, IServiceApi
    {
        public ServiceApi(RestProxy proxy) : base(proxy)
        {
        }

        public async Task<IEnumerable<string>> GetCalendarsAsync(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("service/calendars", HttpMethod.Get);
            var result = await _proxy.InvokeAsync<List<string>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<IEnumerable<WorkingHoursDetails>> GetDefaultWorkingHoursAsync(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("service/working-hours", HttpMethod.Get);
            var result = await _proxy.InvokeAsync<IEnumerable<WorkingHoursDetails>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<AppSettingsInfo> GetInfoAsync(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("service", HttpMethod.Get);
            var result = await _proxy.InvokeAsync<AppSettingsInfo>(restRequest, cancellationToken);
            return result;
        }

        public async Task<string> GetVersionAsync(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("service/version", HttpMethod.Get);
            var result = await _proxy.InvokeAsync<string>(restRequest, cancellationToken);
            return result;
        }

        public async Task<WorkingHoursDetails> GetWorkingHoursAsync(string calendar, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(calendar, nameof(calendar));
            var restRequest = new RestRequest("service/working-hours/{calendar}", HttpMethod.Get);
            restRequest.AddSegmentParameter("calendar", calendar);
            var result = await _proxy.InvokeAsync<WorkingHoursDetails>(restRequest, cancellationToken);
            return result;
        }

        public async Task HaltSchedulerAsync(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("service/halt", HttpMethod.Post);
            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task<string> HealthCheckAsync(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("service/health-check", HttpMethod.Get);
            var result = await _proxy.InvokeAsync<string>(restRequest, cancellationToken);
            return result;
        }

        public async Task<PagingResponse<SecurityAuditDetails>> ListSecurityAuditsAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null, int?
            pageNumber = null, int?
            pageSize = null,
            bool ascending = false,
            CancellationToken cancellationToken = default)
        {
            var dateScope = new DateScope(fromDate, toDate);
            var paging = new Paging(pageNumber, pageSize);

            var restRequest = new RestRequest("service/security-audits", HttpMethod.Get)
                .AddQueryDateScope(dateScope)
                .AddQueryPagingParameter(paging)
                .AddQueryParameter("ascending", ascending);

            var result = await _proxy.InvokeAsync<PagingResponse<SecurityAuditDetails>>(restRequest, cancellationToken);
            return result;
        }

        public async Task StartSchedulerAsync(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("service/start", HttpMethod.Post);
            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }
    }
}