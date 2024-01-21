using Planar.Client.Entities;
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
        Task<PagingResponse<HistoryBasicDetails>> List(HistoryFilter? filter = null, CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<HistoryDetails> Get(long id, CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="instanceId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<HistoryDetails> Get(string instanceId, CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<string> GetData(long id, CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<string> GetLog(long id, CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<string> GetException(long id, CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        Task<PagingResponse<HistoryDetails>> GetLast(LastFilter filter, CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        Task<PagingResponse<HistorySummary>> GetSummary(SummaryFilter filter, CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        Task<CounterResponse> GetCounter(CounterFilter filter, CancellationToken cancellationToken = default);
    }
}