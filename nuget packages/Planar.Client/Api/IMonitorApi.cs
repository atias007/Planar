﻿using Planar.Client.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client.Api
{
    public interface IMonitorApi
    {
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

        Task<IEnumerable<MuteDetails>> ListMutes(CancellationToken cancellationToken = default);
    }
}