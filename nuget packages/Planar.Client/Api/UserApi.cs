using Planar.Client.Entities;

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Planar.Client.Api
{
    internal class UserApi : BaseApi, IUserApi
    {
        public UserApi(RestProxy proxy) : base(proxy)
        {
        }

        public async Task<string> AddAsync(User user, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("user", HttpMethod.Post)
                .AddBody(user);

            var result = await _proxy.InvokeAsync<string>(restRequest, cancellationToken);
            return result;
        }

        public async Task DeleteAsync(string username, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(username, nameof(username));

            var restRequest = new RestRequest("user/{username}", HttpMethod.Delete)
                .AddSegmentParameter("username", username);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task ExcludeFromGroupAsync(string username, string group, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(group, nameof(group));
            ValidateMandatory(username, nameof(username));

            var restRequest = new RestRequest("group/{name}/user/{username}", HttpMethod.Delete)
              .AddSegmentParameter("name", group)
              .AddSegmentParameter("username", username);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task<UserDetails> GetAsync(string username, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(username, nameof(username));

            var restRequest = new RestRequest("user/{username}", HttpMethod.Get)
               .AddSegmentParameter("username", username);

            var result = await _proxy.InvokeAsync<UserDetails>(restRequest, cancellationToken);
            return result;
        }

        public async Task<Roles> GetRoleAsync(string username, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(username, nameof(username));

            var restRequest = new RestRequest("user/{username}/role", HttpMethod.Get)
               .AddSegmentParameter("username", username);

            var title = await _proxy.InvokeAsync<string>(restRequest, cancellationToken);
            Enum.TryParse<Roles>(title, ignoreCase: true, out var result);
            return result;
        }

        public async Task JoinToGroupAsync(string username, string group, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(group, nameof(group));
            ValidateMandatory(username, nameof(username));

            var restRequest = new RestRequest("group/{name}/user/{username}", HttpPatchMethod)
               .AddSegmentParameter("name", group)
               .AddSegmentParameter("username", username);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task<PagingResponse<UserBasicDetails>> ListAsync(int? pageNumber = null, int? pageSize = null, CancellationToken cancellationToken = default)
        {
            var paging = new Paging(pageNumber, pageSize);
            var restRequest = new RestRequest("user", HttpMethod.Get)
                .AddQueryPagingParameter(paging);

            var result = await _proxy.InvokeAsync<PagingResponse<UserBasicDetails>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<string> ResetPasswordAsync(string username, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(username, nameof(username));

            var restRequest = new RestRequest("user/{username}/reset-password", HttpPatchMethod)
                .AddSegmentParameter("username", username);

            var result = await _proxy.InvokeAsync<string>(restRequest, cancellationToken);
            return result;
        }

        public async Task SetPasswordAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(username, nameof(username));
            ValidateMandatory(password, nameof(password));

            var restRequest = new RestRequest("user/{username}/password", HttpPatchMethod)
                .AddSegmentParameter("username", username)
                .AddBody(new
                {
                    password,
                    name = username
                });

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

#if NETSTANDARD2_0

        public async Task UpdateAsync(string username, string propertyName, string propertyValue, CancellationToken cancellationToken = default)
#else
        public async Task UpdateAsync(string username, string propertyName, string? propertyValue, CancellationToken cancellationToken = default)
#endif
        {
            ValidateMandatory(username, nameof(username));
            ValidateMandatory(propertyName, nameof(propertyName));

            var restRequest = new RestRequest("group", HttpPatchMethod)
               .AddBody(new
               {
                   name = username,
                   propertyName,
                   propertyValue
               });

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }
    }
}