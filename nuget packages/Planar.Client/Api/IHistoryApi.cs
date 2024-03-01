using Planar.Client.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client
{
    /// <summary>
    /// Represents a collection of functions to interact with the History API endpoints
    /// </summary>
    public interface IHistoryApi
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        Task<PagingResponse<HistoryBasicDetails>> ListAsync(ListHistoryFilter? filter = null, CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<HistoryDetails> GetAsync(long id, CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="instanceId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<HistoryDetails> GetAsync(string instanceId, CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<string> GetDataAsync(long id, CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<string> GetLogAsync(long id, CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<string> GetExceptionAsync(long id, CancellationToken cancellationToken = default);

        Task<PagingResponse<LastRunDetails>> LastAsync(
            int? lastDays = null,
            int? pageNumber = null,
            int? pageSize = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        Task<PagingResponse<HistorySummary>> SummaryAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? pageNumber = null,
            int? pageSize = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        Task<CounterResponse> GetCounterAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default);
    }
}