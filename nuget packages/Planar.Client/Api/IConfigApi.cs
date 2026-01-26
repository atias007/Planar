using Planar.Client.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client.Api
{
    public interface IConfigApi
    {
        Task<IEnumerable<GlobalConfig>> ListAsync(CancellationToken cancellationToken = default);

        Task<IEnumerable<KeyValueItem>> ListFlatAsync(CancellationToken cancellationToken = default);

        Task<GlobalConfig> GetAsync(string key, CancellationToken cancellationToken = default);

#if NETSTANDARD2_0

        Task AddAsync(
            string key,
            string value,
            string sourceUrl = null,
            ConfigType? configType = null,
            CancellationToken cancellationToken = default);

        Task UpdateAsync(
            string key,
            string value,
            string sourceUrl = null,
            ConfigType? configType = null,
            CancellationToken cancellationToken = default);

#else
        Task AddAsync(
            string key,
            string? value,
            string? sourceUrl = null,
            ConfigType? configType = null,
            CancellationToken cancellationToken = default);

        Task UpdateAsync(
            string key,
            string? value,
            string? sourceUrl = null,
            ConfigType? configType = null,
            CancellationToken cancellationToken = default);
#endif

        Task DeleteAsync(string key, CancellationToken cancellationToken = default);

        Task FlushAsync(CancellationToken cancellationToken = default);
    }
}