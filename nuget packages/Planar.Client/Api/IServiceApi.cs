using Planar.Client.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client.Api
{
    public interface IServiceApi
    {
        /// <summary>
        /// Applies the given definition to the service. This method is used to update the service's configuration or state based on the provided definition.
        /// </summary>
        /// <param name="definition">The YAML string that contains the service definition to be applied.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The result of the apply operation as an <see cref="ApplyResponse"/>.</returns>
        Task<ApplyResponse> ApplyAsync(string definition, CancellationToken cancellationToken = default);

        Task<string> GetVersionAsync(CancellationToken cancellationToken = default);

        Task<AppSettingsInfo> GetInfoAsync(CancellationToken cancellationToken = default);

        Task<IEnumerable<AgentDetails>> GetAgentDetailsAsync(CancellationToken cancellationToken = default);

        Task<ServiceHealthCheck> HealthCheckAsync(CancellationToken cancellationToken = default);

        Task<IEnumerable<string>> GetCalendarsAsync(CancellationToken cancellationToken = default);

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