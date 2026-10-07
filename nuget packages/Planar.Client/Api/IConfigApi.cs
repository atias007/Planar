using Planar.Client.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client.Api
{
    public interface IConfigApi
    {
        /// <summary>
        /// Applies the configuration definition to the system. The definition is a YAML string that contains the configuration settings to be applied.
        /// </summary>
        /// <param name="definition">The YAML string that contains the configuration settings to be applied.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The result of the apply operation as an <see cref="ApplyResponse"/>.</returns>
        Task<ApplyResponse> ApplyAsync(string definition, CancellationToken cancellationToken = default);

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
            ConfigType configType = ConfigType.String,
            bool isSecret = false,
            CancellationToken cancellationToken = default);

        Task UpdateAsync(
            string key,
            string value,
            string sourceUrl = null,
            ConfigType configType = ConfigType.String,
            bool isSecret = false,
            CancellationToken cancellationToken = default);

#else
        Task AddAsync(
            string key,
            string? value,
            string? sourceUrl = null,
            ConfigType configType = ConfigType.String,
            bool isSecret = false,
            CancellationToken cancellationToken = default);

        Task UpdateAsync(
            string key,
            string? value,
            string? sourceUrl = null,
            ConfigType configType = ConfigType.String,
            bool isSecret = false,
            CancellationToken cancellationToken = default);

#endif

        Task DeleteAsync(string key, CancellationToken cancellationToken = default);

        Task FlushAsync(CancellationToken cancellationToken = default);
    }
}