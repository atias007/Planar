using Planar.Client.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client.Api
{
    public interface IReportApi
    {
        Task<IEnumerable<ReportsStatus>> GetAsync(ReportNames report, CancellationToken cancellationToken = default);

        Task RunAsync(ReportNames report, ReportPeriods? period, string? group, CancellationToken cancellationToken = default);

        Task RunAsync(ReportNames report, DateTime? fromDate, DateTime? toDate, string? group, CancellationToken cancellationToken = default);

        Task EnableAsync(ReportNames report, ReportPeriods period, string? group, CancellationToken cancellationToken = default);

        Task DisableAsync(ReportNames report, ReportPeriods period, CancellationToken cancellationToken = default);

        Task SetGroupAsync(ReportNames report, ReportPeriods period, string group, CancellationToken cancellationToken = default);

        Task SetHourAsync(ReportNames report, ReportPeriods period, ushort hour, CancellationToken cancellationToken = default);
    }
}