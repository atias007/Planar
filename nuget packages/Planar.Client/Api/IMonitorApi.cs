using Planar.Client.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client.Api
{
    public interface IMonitorApi
    {
        /// <summary>
        /// Applies the monitor definition to the system. This method will add, update, or delete monitors based on the provided definition.
        /// </summary>
        /// <param name="definition">The YAML string that contains the monitor definition to be applied.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The result of the apply operation as an <see cref="ApplyResponse"/>.</returns>
        Task<ApplyResponse> ApplyAsync(string definition, CancellationToken cancellationToken = default);

        Task<PagingResponse<MonitorDetails>> ListAsync(
            int? pageNumber = null,
            int? pageSize = null,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<MonitorDetails>> ListByJobAsync(string jobId, CancellationToken cancellationToken = default);

        Task<IEnumerable<MonitorDetails>> ListByGroupAsync(string group, CancellationToken cancellationToken = default);

        Task<MonitorDetails> GetAsync(int id, CancellationToken cancellationToken = default);

        Task<MonitorAlertDetails> GetAlertAsync(int alertId, CancellationToken cancellationToken = default);

        Task<PagingResponse<MonitorAlertBasicDetails>> ListAlertsAsync(ListAlertsFilter filter, CancellationToken cancellationToken = default);

        Task<IEnumerable<MonitorEvent>> ListEventsAsync(CancellationToken cancellationToken = default);

        Task<int> AddAsync(AddMonitorRequest request, CancellationToken cancellationToken = default);

#if NETSTANDARD2_0

        Task UpdateAsync(int id, string propertyName, string propertyValue, CancellationToken cancellationToken = default);

#else
        Task UpdateAsync(int id, string propertyName, string? propertyValue, CancellationToken cancellationToken = default);
#endif

        Task DeleteAsync(int id, CancellationToken cancellationToken = default);

        Task<IEnumerable<HookDetails>> ListHooksAsync(CancellationToken cancellationToken = default);

        Task MuteAsync(string jobId, int monitorId, DateTime dueDate, CancellationToken cancellationToken = default);

        Task Unmute(string jobId, int monitorId, CancellationToken cancellationToken = default);

        Task<IEnumerable<MuteDetails>> ListMutesAsync(CancellationToken cancellationToken = default);

        Task AddDistributionGroupAsync(int monitorId, string groupName, CancellationToken cancellationToken = default);

        Task RemoveDistributionGroupAsync(int monitorId, string groupName, CancellationToken cancellationToken = default);

        Task AddMonitorHookAsync(int monitorId, string hook, CancellationToken cancellationToken = default);

        Task RemoveMonitorHookAsync(int monitorId, string hook, CancellationToken cancellationToken = default);
    }
}