﻿using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.CliGeneral;
using Planar.CLI.Entities;
using Planar.CLI.Proxy;
using RestSharp;
using System;
using System.Collections.Generic;
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
            var wrapper = await FillCliAddGroupRequest(request, cancellationToken);
            if (!wrapper.IsSuccessful)
            {
                return new CliActionResponse(wrapper.FailResponse);
            }

            var body = new { request.Name, Role = request.Role.ToString() };
            var restRequest = new RestRequest("group", Method.Post)
                .AddBody(body);
            var result = await RestProxy.Invoke(restRequest, cancellationToken);
            return new CliActionResponse(result);
        }

        [Action("get")]
        public static async Task<CliActionResponse> GetByName(CliByNameRequest request, CancellationToken cancellationToken = default)
        {
            var wrapper = await FillGetRequest(request, cancellationToken);
            if (!wrapper.IsSuccessful)
            {
                return new CliActionResponse(wrapper.FailResponse);
            }

            var restRequest = new RestRequest("group/{name}", Method.Get)
                .AddParameter("name", request.Name ?? string.Empty, ParameterType.UrlSegment);

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
        public static async Task<CliActionResponse> Get(CliPagingRequest request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("group", Method.Get)
                .AddQueryPagingParameter(request);

            var result = await RestProxy.Invoke<PagingResponse<GroupInfo>>(restRequest, cancellationToken);

            if (result.IsSuccessful)
            {
                var table = CliTableExtensions.GetTable(result.Data);
                return new CliActionResponse(result, table);
            }

            return new CliActionResponse(result);
        }

        [Action("remove")]
        [Action("delete")]
        public static async Task<CliActionResponse> RemoveById(CliByNameRequest request, CancellationToken cancellationToken = default)
        {
            var wrapper = await FillGetRequest(request, cancellationToken);
            if (!wrapper.IsSuccessful)
            {
                return new CliActionResponse(wrapper.FailResponse);
            }

            if (!ConfirmAction($"remove group name {request.Name}")) { return CliActionResponse.Empty; }

            var restRequest = new RestRequest("group/{name}", Method.Delete)
                .AddParameter("name", request.Name ?? string.Empty, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke(restRequest, cancellationToken);
            return new CliActionResponse(result);
        }

        [Action("update")]
        public static async Task<CliActionResponse> Update(CliUpdateEntityByNameRequest request, CancellationToken cancellationToken = default)
        {
            var wrapper = await FillGetRequest(request, cancellationToken);
            if (!wrapper.IsSuccessful)
            {
                return new CliActionResponse(wrapper.FailResponse);
            }

            FillRequiredString(request, nameof(request.PropertyName));
            FillOptionalString(request, nameof(request.PropertyValue));

            var restRequest = new RestRequest("group", Method.Patch)
               .AddBody(request);

            var result = await RestProxy.Invoke(restRequest, cancellationToken);
            return new CliActionResponse(result);
        }

        [Action("set-role")]
        public static async Task<CliActionResponse> SetRole(CliSetRoleRequest request, CancellationToken cancellationToken = default)
        {
            var wrapper = await FillCliSetRoleRequest(request, cancellationToken);
            if (!wrapper.IsSuccessful)
            {
                return new CliActionResponse(wrapper.FailResponse);
            }

            if (request.Role == null) { return CliActionResponse.Empty; }

            if (!ConfirmAction($"change group role to {request.Role.ToString().ToLower()}")) { return CliActionResponse.Empty; }

            var restRequest = new RestRequest("group/{name}/role/{role}", Method.Patch)
               .AddUrlSegment("name", request.Name)
               .AddUrlSegment("role", request.Role);

            var result = await RestProxy.Invoke(restRequest, cancellationToken);
            return new CliActionResponse(result);
        }

        [Action("join")]
        public static async Task<CliActionResponse> AddUserToGroup(CliGroupToUserRequest request, CancellationToken cancellationToken = default)
        {
            var wrapper = await FillCliGroupToUserRequest1(request, cancellationToken);
            if (!wrapper.IsSuccessful)
            {
                return new CliActionResponse(wrapper.FailResponse);
            }

            var restRequest = new RestRequest("group/{name}/user/{username}", Method.Patch)
               .AddParameter("name", request.Name, ParameterType.UrlSegment)
               .AddParameter("username", request.Username, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke(restRequest, cancellationToken);
            return new CliActionResponse(result);
        }

        [Action("exclude")]
        public static async Task<CliActionResponse> RemoveUserFromGroup(CliGroupToUserRequest request, CancellationToken cancellationToken = default)
        {
            var wrapper = await FillCliGroupToUserRequest2(request, cancellationToken);
            if (!wrapper.IsSuccessful)
            {
                return new CliActionResponse(wrapper.FailResponse);
            }

            var restRequest = new RestRequest("group/{name}/user/{username}", Method.Delete)
               .AddParameter("name", request.Name, ParameterType.UrlSegment)
               .AddParameter("username", request.Username, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke(restRequest, cancellationToken);
            return new CliActionResponse(result);
        }

        private static async Task<CliPromptWrapper> FillCliAddGroupRequest(CliAddGroupRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                request.Name = CollectCliValue("name", true, 2, 50) ?? string.Empty;
            }

            if (request.Role == null)
            {
                var p1 = await CliPromptUtil.Roles(cancellationToken);
                if (!p1.IsSuccessful) { return p1; }
                request.Role = p1.Value;
            }

            return CliPromptWrapper.Success;
        }

        private static async Task<CliPromptWrapper> FillGetRequest(ICliByNameRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                var p1 = await CliPromptUtil.Groups(cancellationToken);
                if (!p1.IsSuccessful) { return p1; }
                request.Name = p1.Value ?? string.Empty;
            }

            return CliPromptWrapper.Success;
        }

        private static async Task<CliPromptWrapper> FillCliSetRoleRequest(CliSetRoleRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                var p1 = await CliPromptUtil.Groups(cancellationToken);
                if (!p1.IsSuccessful) { return p1; }
                request.Name = p1.Value ?? string.Empty;
            }

            if (string.IsNullOrEmpty(request.Role))
            {
                var p1 = await CliPromptUtil.Roles(cancellationToken);
                if (!p1.IsSuccessful) { return p1; }
                request.Role = p1.Value.ToString();
            }

            return CliPromptWrapper.Success;
        }

        private static async Task<CliPromptWrapper> FillCliGroupToUserRequest1(CliGroupToUserRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.Name))
            {
                var p1 = await CliPromptUtil.Groups(cancellationToken);
                if (!p1.IsSuccessful) { return p1; }
                request.Name = p1.Value ?? string.Empty;
            }

            if (string.IsNullOrEmpty(request.Username))
            {
                var p1 = await CliPromptUtil.UsersNotInGroup(request.Name, cancellationToken);
                if (!p1.IsSuccessful) { return p1; }
                request.Username = p1.Value ?? string.Empty;
            }

            return CliPromptWrapper.Success;
        }

        private static async Task<CliPromptWrapper> FillCliGroupToUserRequest2(CliGroupToUserRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.Name))
            {
                var p1 = await CliPromptUtil.Groups(cancellationToken);
                if (!p1.IsSuccessful) { return p1; }
                request.Name = p1.Value ?? string.Empty;
            }

            if (string.IsNullOrEmpty(request.Username))
            {
                var p1 = await CliPromptUtil.UsersInGroup(request.Name, cancellationToken);
                if (!p1.IsSuccessful) { return p1; }
                request.Username = p1.Value ?? string.Empty;
            }

            return CliPromptWrapper.Success;
        }
    }
}