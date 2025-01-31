using Planar.Client.Entities;
using RestSharp;
using System;
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
            var restRequest = new RestRequest("user", Method.Post)
                .AddBody(user);

            var result = await _proxy.InvokeAsync<string>(restRequest, cancellationToken);
            return result;
        }

        public async Task DeleteAsync(string username, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(username, nameof(username));

            var restRequest = new RestRequest("user/{username}", Method.Delete)
                .AddParameter("username", username, ParameterType.UrlSegment);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task ExcludeFromGroupAsync(string username, string group, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(group, nameof(group));
            ValidateMandatory(username, nameof(username));

            var restRequest = new RestRequest("group/{name}/user/{username}", Method.Delete)
              .AddParameter("name", group, ParameterType.UrlSegment)
              .AddParameter("username", username, ParameterType.UrlSegment);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task<UserDetails> GetAsync(string username, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(username, nameof(username));

            var restRequest = new RestRequest("user/{username}", Method.Get)
               .AddParameter("username", username, ParameterType.UrlSegment);

            var result = await _proxy.InvokeAsync<UserDetails>(restRequest, cancellationToken);
            return result;
        }

        public async Task<Roles> GetRoleAsync(string username, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(username, nameof(username));

            var restRequest = new RestRequest("user/{username}/role", Method.Get)
               .AddParameter("username", username, ParameterType.UrlSegment);

            var title = await _proxy.InvokeAsync<string>(restRequest, cancellationToken);
            Enum.TryParse<Roles>(title, ignoreCase: true, out var result);
            return result;
        }

        public async Task JoinToGroupAsync(string username, string group, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(group, nameof(group));
            ValidateMandatory(username, nameof(username));

            var restRequest = new RestRequest("group/{name}/user/{username}", Method.Patch)
               .AddParameter("name", group, ParameterType.UrlSegment)
               .AddParameter("username", username, ParameterType.UrlSegment);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task<PagingResponse<UserBasicDetails>> ListAsync(int? pageNumber = null, int? pageSize = null, CancellationToken cancellationToken = default)
        {
            var paging = new Paging(pageNumber, pageSize);
            var restRequest = new RestRequest("user", Method.Get)
                .AddQueryPagingParameter(paging);

            var result = await _proxy.InvokeAsync<PagingResponse<UserBasicDetails>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<string> ResetPasswordAsync(string username, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(username, nameof(username));

            var restRequest = new RestRequest("user/{username}/reset-password", Method.Patch)
                .AddParameter("username", username, ParameterType.UrlSegment);

            var result = await _proxy.InvokeAsync<string>(restRequest, cancellationToken);
            return result;
        }

        public async Task SetPasswordAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(username, nameof(username));
            ValidateMandatory(password, nameof(password));

            var restRequest = new RestRequest("user/{username}/password", Method.Patch)
                .AddParameter("username", username, ParameterType.UrlSegment)
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

            var restRequest = new RestRequest("group", Method.Patch)
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