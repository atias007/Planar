using Planar.Client.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client
{
    /// <summary>
    /// Represents a collection of functions to interact with the History API endpoints
    /// </summary>
    public interface ITriggerApi
    {
        Task<TriggerBasicDetails> GetAsync(string id, CancellationToken cancellationToken = default);

        Task<TriggerBasicDetails> ListAsync(string jobId, CancellationToken cancellationToken = default);

        Task DeleteAsync(string id, CancellationToken cancellationToken = default);

        Task UpdateCronExpressionAsync(string id, string cronExpression, CancellationToken cancellationToken = default);

        Task UpdateIntervalAsync(string id, TimeSpan interval, CancellationToken cancellationToken = default);

        Task PauseAsync(string id, CancellationToken cancellationToken = default);

        Task ResumeAsync(string id, CancellationToken cancellationToken = default);

        Task PutDataAsync(string id, string key, string value, CancellationToken cancellationToken = default);

        Task DeleteDataAsync(string id, string key, CancellationToken cancellationToken = default);

        Task<string> GetCronDescriptionAsync(string expression, CancellationToken cancellationToken = default);

        Task<IEnumerable<PausedTrigger>> GetPausedAsync(CancellationToken cancellationToken = default);
    }
}