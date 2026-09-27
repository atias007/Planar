using Planar.Client.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client.Api
{
    public interface IUserApi
    {
        /// <summary>
        /// Apply user definition to the system. The definition is a JSON string that contains an array of user objects. Each user object can have the following properties: username, password, roles, and groups. The method will add new users, update existing users, and delete users that are not in the definition.
        /// </summary>
        /// <param name="definition">The JSON string that contains the user definition to be applied.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The result of the apply operation as an <see cref="ApplyResponse"/>.</returns>
        Task<ApplyResponse> ApplyAsync(string definition, CancellationToken cancellationToken = default);

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