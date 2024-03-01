using Planar.Client.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client.Api
{
    public interface IMetricsApi
    {
        Task RebuildAsync(CancellationToken cancellationToken = default);

        Task<IEnumerable<JobMetrics>> ListMetricsAsync(string jobId, CancellationToken cancellationToken = default);

        Task<JobCounters> ListMetricsAsync(DateTime? fromDate = null, CancellationToken cancellationToken = default);

        Task<PagingResponse<ConcurrentExecutionDetails>> ListConcurrentExecutionAsync(ConcurrentFilter filter, CancellationToken cancellationToken = default);

        Task<MaxConcurrentExecution> GetMaxConcurrentExecutionAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    }
}