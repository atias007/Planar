﻿using Planar.Client.Entities;
using System;
using System.IO;
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

#if NETSTANDARD2_0

        Task<PagingResponse<HistoryBasicDetails>> ListAsync(ListHistoryFilter filter = null, CancellationToken cancellationToken = default);

#else
        Task<PagingResponse<HistoryBasicDetails>> ListAsync(ListHistoryFilter? filter = null, CancellationToken cancellationToken = default);
#endif

        /// <summary>
        ///
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
#if NETSTANDARD2_0

        Task<string> ODataAsync(ODataFilter filter = null, CancellationToken cancellationToken = default);

#else
        Task<string> ODataAsync(ODataFilter? filter = null, CancellationToken cancellationToken = default);
#endif

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
        Task<Stream> GetDataAsync(long id, CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        Task<Stream> GetDataAsync(string instanceId, CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<Stream> GetLogAsync(long id, CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        Task<Stream> GetLogAsync(string instanceId, CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<Stream> GetExceptionAsync(long id, CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        Task<Stream> GetExceptionAsync(string instanceId, CancellationToken cancellationToken = default);

#if NETSTANDARD2_0

        Task<PagingResponse<LastRunDetails>> LastAsync(LastHistoryFilter filter = null, CancellationToken cancellationToken = default);

#else
        Task<PagingResponse<LastRunDetails>> LastAsync(LastHistoryFilter? filter = null, CancellationToken cancellationToken = default);
#endif

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