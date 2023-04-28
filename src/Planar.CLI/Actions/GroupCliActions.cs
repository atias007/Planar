using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.CliGeneral;
using Planar.CLI.Entities;
using Planar.CLI.General;
using Planar.CLI.Proxy;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("group", "Actions to handle groups", Synonyms = "groups")]
    public class GroupCliActions : BaseCliAction<GroupCliActions>
    {
        [Action("add")]
        public static async Task<CliActionResponse> Add(CliAddGroupRequest request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("group", Method.Post)
                .AddBody(request);
            var result = await RestProxy.Invoke(restRequest, cancellationToken);
            return new CliActionResponse(result);
        }

        [Action("get")]
        public static async Task<CliActionResponse> GetById(CliGetByIdRequest request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("group/{id}", Method.Get)
                .AddParameter("id", request.Id, ParameterType.UrlSegment);

            return await ExecuteEntity<GroupDetails>(restRequest, cancellationToken);
        }

        [Action("roles")]
        public static async Task<CliActionResponse> GetRoles(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("group/roles", Method.Get);

            return await ExecuteEntity<List<string>>(restRequest, cancellationToken);
        }

        [Action("ls")]
        [Action("list")]
        public static async Task<CliActionResponse> Get(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("group", Method.Get);
            var result = await RestProxy.Invoke<List<GroupInfo>>(restRequest, cancellationToken);

            if (result.IsSuccessful)
            {
                var table = CliTableExtensions.GetTable(result.Data);
                return new CliActionResponse(result, table);
            }

            return new CliActionResponse(result);
        }

        [Action("remove")]
        [Action("delete")]
        public static async Task<CliActionResponse> RemoveById(CliGetByIdRequest request, CancellationToken cancellationToken = default)
        {
            if (!ConfirmAction($"remove group id {request.Id}")) { return CliActionResponse.Empty; }

            var restRequest = new RestRequest("group/{id}", Method.Delete)
                .AddParameter("id", request.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke(restRequest, cancellationToken);
            return new CliActionResponse(result);
        }

        [Action("update")]
        public static async Task<CliActionResponse> Update(CliUpdateEntityRequest request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("group", Method.Patch)
               .AddBody(request);

            var result = await RestProxy.Invoke(restRequest, cancellationToken);
            return new CliActionResponse(result);
        }

        [Action("set-role")]
        public static async Task<CliActionResponse> SetRole(CliSetRoleRequest request, CancellationToken cancellationToken = default)
        {
            if (!ConfirmAction($"change group role to {request.Role}")) { return CliActionResponse.Empty; }

            var restRequest = new RestRequest("group/{id}/role/{role}", Method.Patch)
               .AddUrlSegment("id", request.GroupId)
               .AddUrlSegment("role", (int)request.Role);

            var result = await RestProxy.Invoke(restRequest, cancellationToken);
            return new CliActionResponse(result);
        }

        [Action("join")]
        public static async Task<CliActionResponse> AddUserToGroup(CliUserToGroupRequest request, CancellationToken cancellationToken = default)
        {
            var wrapper = await FillCliUserToGroupRequest(request, cancellationToken);
            if (!wrapper.IsSuccessful)
            {
                return new CliActionResponse(wrapper.FailResponse);
            }

            var restRequest = new RestRequest("group/{id}/user/{userId}", Method.Patch)
               .AddParameter("id", request.GroupId, ParameterType.UrlSegment)
               .AddParameter("userId", request.UserId, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke(restRequest, cancellationToken);
            return new CliActionResponse(result);
        }

        [Action("exclude")]
        public static async Task<CliActionResponse> RemoveUserFromGroup(CliUserToGroupRequest request, CancellationToken cancellationToken = default)
        {
            var wrapper = await FillCliUserToGroupRequest(request, cancellationToken);
            if (!wrapper.IsSuccessful)
            {
                return new CliActionResponse(wrapper.FailResponse);
            }

            var restRequest = new RestRequest("group/{id}/user/{userId}", Method.Delete)
               .AddParameter("id", request.GroupId, ParameterType.UrlSegment)
               .AddParameter("userId", request.UserId, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke(restRequest, cancellationToken);
            return new CliActionResponse(result);
        }

        private static async Task<CliPromptWrapper> FillCliUserToGroupRequest(CliUserToGroupRequest request, CancellationToken cancellationToken)
        {
            if (request.GroupId == 0)
            {
                var p1 = await CliPromptUtil.Groups(cancellationToken);
                if (!p1.IsSuccessful) { return p1; }
                request.GroupId = p1.Value;
            }

            if (request.UserId == 0)
            {
                var p1 = await CliPromptUtil.Users(cancellationToken);
                if (!p1.IsSuccessful) { return p1; }
                request.UserId = p1.Value;
            }

            return CliPromptWrapper.Success;
        }
    }
}