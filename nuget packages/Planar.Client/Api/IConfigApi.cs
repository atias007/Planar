using Planar.Client.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client.Api
{
    public interface IConfigApi
    {
        Task<PagingResponse<GlobalConfig>> ListAsync(
            int? pageNumber = null,
            int? pageSize = null,
            CancellationToken cancellationToken = default);

        Task<PagingResponse<KeyValueItem>> ListFlatAsync(
            int? pageNumber = null,
            int? pageSize = null,
            CancellationToken cancellationToken = default);

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
            CancellationToken cancellationToken = default);

        Task AddSecretAsync(
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
            CancellationToken cancellationToken = default);

        Task AddSecretAsync(
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