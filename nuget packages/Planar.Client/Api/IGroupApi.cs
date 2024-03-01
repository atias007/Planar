using Planar.Client.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client.Api
{
    public interface IGroupApi
    {
        Task AddAsync(Group group, CancellationToken cancellationToken = default);

        Task<GroupDetails> GetAsync(string name, CancellationToken cancellationToken = default);

        Task<PagingResponse<GroupBasicDetails>> ListAsync(int? pageNumber = null, int? pageSize = null, CancellationToken cancellationToken = default);

        Task<IEnumerable<string>> ListRolesAsync(CancellationToken cancellationToken = default);

        Task DeleteAsync(string name, CancellationToken cancellationToken = default);

        Task UpdateAsync(string name, string propertyName, string? propertyValue, CancellationToken cancellationToken = default);

        Task JoinUserAsync(string name, string username, CancellationToken cancellationToken = default);

        Task ExcludeUserAsync(string name, string username, CancellationToken cancellationToken = default);

        Task SetRoleAsync(string name, Roles role, CancellationToken cancellationToken = default);
    }
}