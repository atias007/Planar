using Planar.Client.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client.Api
{
    public interface IUserApi
    {
        Task<string> AddAsync(User user, CancellationToken cancellationToken = default);

#if NETSTANDARD2_0

        Task UpdateAsync(string username, string propertyName, string propertyValue, CancellationToken cancellationToken = default);

#else
        Task UpdateAsync(string username, string propertyName, string? propertyValue, CancellationToken cancellationToken = default);
#endif

        Task<UserDetails> GetAsync(string username, CancellationToken cancellationToken = default);

        Task<Roles> GetRoleAsync(string username, CancellationToken cancellationToken = default);

        Task<PagingResponse<UserBasicDetails>> ListAsync(int? pageNumber = null, int? pageSize = null, CancellationToken cancellationToken = default);

        Task DeleteAsync(string username, CancellationToken cancellationToken = default);

        Task<string> ResetPasswordAsync(string username, CancellationToken cancellationToken = default);

        Task SetPasswordAsync(string username, string password, CancellationToken cancellationToken = default);

        Task JoinToGroupAsync(string username, string group, CancellationToken cancellationToken = default);

        Task ExcludeFromGroupAsync(string username, string group, CancellationToken cancellationToken = default);
    }
}