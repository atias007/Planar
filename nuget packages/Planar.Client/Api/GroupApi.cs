using Planar.Client.Entities;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client.Api
{
    internal class GroupApi : BaseApi, IGroupApi
    {
        public GroupApi(RestProxy proxy) : base(proxy)
        {
        }

        public async Task AddAsync(Group group, CancellationToken cancellationToken = default)
        {
            var body = new
            {
                group.Name,
                group.AdditionalField1,
                group.AdditionalField2,
                group.AdditionalField3,
                group.AdditionalField4,
                group.AdditionalField5,
                Role = group.Role.ToString().ToLower()
            };

            var restRequest = new RestRequest("group", HttpMethod.Post)
                .AddBody(body);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task DeleteAsync(string name, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(name, nameof(name));
            var restRequest = new RestRequest("group/{name}", HttpMethod.Delete)
                .AddSegmentParameter("name", name);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task ExcludeUserAsync(string name, string username, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(name, nameof(name));
            ValidateMandatory(username, nameof(username));

            var restRequest = new RestRequest("group/{name}/user/{username}", HttpMethod.Delete)
              .AddSegmentParameter("name", name)
              .AddSegmentParameter("username", username);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task<GroupDetails> GetAsync(string name, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(name, nameof(name));

            var restRequest = new RestRequest("group/{name}", HttpMethod.Get)
                .AddSegmentParameter("name", name);

            var result = await _proxy.InvokeAsync<GroupDetails>(restRequest, cancellationToken);
            return result;
        }

        public async Task JoinUserAsync(string name, string username, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(name, nameof(name));
            ValidateMandatory(username, nameof(username));

            var restRequest = new RestRequest("group/{name}/user/{username}", HttpPatchMethod)
               .AddSegmentParameter("name", name)
               .AddSegmentParameter("username", username);

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

        public async Task<PagingResponse<GroupBasicDetails>> ListAsync(int? pageNumber = null, int? pageSize = null, CancellationToken cancellationToken = default)
        {
            var paging = new Paging(pageNumber, pageSize);

            var restRequest = new RestRequest("group", HttpMethod.Get)
                .AddQueryPagingParameter(paging);

            var result = await _proxy.InvokeAsync<PagingResponse<GroupBasicDetails>>(restRequest, cancellationToken);
            return result;
        }

        public async Task<IEnumerable<string>> ListRolesAsync(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("group/roles", HttpMethod.Get);
            var result = await _proxy.InvokeAsync<List<string>>(restRequest, cancellationToken);
            return result;
        }

        public async Task SetRoleAsync(string name, Roles role, CancellationToken cancellationToken = default)
        {
            ValidateMandatory(name, nameof(name));

            var restRequest = new RestRequest("group/{name}/role/{role}", HttpPatchMethod)
               .AddSegmentParameter("name", name)
               .AddSegmentParameter("role", role.ToString().ToLower());

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }

#if NETSTANDARD2_0

        public async Task UpdateAsync(string name, string propertyName, string propertyValue, CancellationToken cancellationToken = default)

#else
        public async Task UpdateAsync(string name, string propertyName, string? propertyValue, CancellationToken cancellationToken = default)

#endif
        {
            ValidateMandatory(name, nameof(name));
            ValidateMandatory(propertyName, nameof(propertyName));

            var restRequest = new RestRequest("group", HttpPatchMethod)
               .AddBody(new
               {
                   name,
                   propertyName,
                   propertyValue
               });

            await _proxy.InvokeAsync(restRequest, cancellationToken);
        }
    }
}