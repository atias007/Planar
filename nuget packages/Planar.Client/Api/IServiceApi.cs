using Planar.Client.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client.Api
{
    public interface IServiceApi
    {
        Task<string> GetVersionAsync(CancellationToken cancellationToken = default);

        Task<AppSettingsInfo> GetInfoAsync(CancellationToken cancellationToken = default);

        Task<string> HealthCheckAsync(CancellationToken cancellationToken = default);

        Task<IEnumerable<string>> GetCalendarsAsync(CancellationToken cancellationToken = default);

        Task HaltSchedulerAsync(CancellationToken cancellationToken = default);

        Task StartSchedulerAsync(CancellationToken cancellationToken = default);

        Task<PagingResponse<SecurityAuditDetails>> ListSecurityAuditsAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? pageNumber = null,
            int? pageSize = null,
            bool ascending = false,
            CancellationToken cancellationToken = default);

        Task<WorkingHoursDetails> GetWorkingHoursAsync(string calendar, CancellationToken cancellationToken = default);

        Task<IEnumerable<WorkingHoursDetails>> GetDefaultWorkingHoursAsync(CancellationToken cancellationToken = default);
    }
}