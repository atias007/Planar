using Planar.Client.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client.Api
{
    public interface IGroupApi
    {
        /// <summary>
        /// Applies YAML group definitions, creating or updating the groups present in the definition and synchronizing their listed users.
        /// </summary>
        /// <param name="definition">The YAML string that contains the group definition to be applied.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The result of the apply operation as an <see cref="ApplyResponse"/>.</returns>
        Task<ApplyResponse> ApplyAsync(string definition, CancellationToken cancellationToken = default);

        Task AddAsync(Group group, CancellationToken cancellationToken = default);

        Task<GroupDetails> GetAsync(string name, CancellationToken cancellationToken = default);

        Task<PagingResponse<GroupBasicDetails>> ListAsync(int? pageNumber = null, int? pageSize = null, CancellationToken cancellationToken = default);

        Task<IEnumerable<string>> ListRolesAsync(CancellationToken cancellationToken = default);

        Task DeleteAsync(string name, CancellationToken cancellationToken = default);

#if NETSTANDARD2_0

        Task UpdateAsync(string name, string propertyName, string propertyValue, CancellationToken cancellationToken = default);

#else
        Task UpdateAsync(string name, string propertyName, string? propertyValue, CancellationToken cancellationToken = default);
#endif

        Task JoinUserAsync(string name, string username, CancellationToken cancellationToken = default);

        Task ExcludeUserAsync(string name, string username, CancellationToken cancellationToken = default);

        Task SetRoleAsync(string name, Roles role, CancellationToken cancellationToken = default);
    }
}