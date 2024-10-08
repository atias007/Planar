﻿using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.CliGeneral;
using Planar.CLI.Entities;
using Planar.CLI.Proxy;
using RestSharp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("user", "Actions to handle users", Synonyms = "users")]
    public class UserCliActions : BaseCliAction<UserCliActions>
    {
        [Action("add")]
        [NullRequest]
        public static async Task<CliActionResponse> AddUser(CliAddUserRequest request, CancellationToken cancellationToken = default)
        {
            request ??= GetCliAddUserRequest();

            var restRequest = new RestRequest("user", Method.Post)
                .AddBody(request);

            return await ExecuteTable<AddUserResponse>(restRequest, CliTableExtensions.GetTable, cancellationToken);
        }

        [Action("get")]
        [NullRequest]
        public static async Task<CliActionResponse> GetUserByUsername(CliByNameRequest request, CancellationToken cancellationToken = default)
        {
            request ??= new CliByNameRequest();
            var wrapper = await FillGetRequest(request, cancellationToken);
            if (!wrapper.IsSuccessful)
            {
                return new CliActionResponse(wrapper.FailResponse);
            }

            var restRequest = new RestRequest("user/{username}", Method.Get)
                .AddParameter("username", request.Name ?? string.Empty, ParameterType.UrlSegment);

            return await ExecuteEntity<UserDetails>(restRequest, cancellationToken);
        }

        [Action("get-role")]
        [NullRequest]
        public static async Task<CliActionResponse> GetUserRoleById(CliByNameRequest request, CancellationToken cancellationToken = default)
        {
            request ??= new CliByNameRequest();
            var wrapper = await FillGetRequest(request, cancellationToken);
            if (!wrapper.IsSuccessful)
            {
                return new CliActionResponse(wrapper.FailResponse);
            }

            var restRequest = new RestRequest("user/{username}/role", Method.Get)
                .AddParameter("username", request.Name ?? string.Empty, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<string>(restRequest, cancellationToken);
            return new CliActionResponse(result, message: result.Data);
        }

        [Action("reset-password")]
        [NullRequest]
        public static async Task<CliActionResponse> ResetUserPassword(CliByNameRequest request, CancellationToken cancellationToken = default)
        {
            request ??= new CliByNameRequest();
            var wrapper = await FillGetRequest(request, cancellationToken);
            if (!wrapper.IsSuccessful)
            {
                return new CliActionResponse(wrapper.FailResponse);
            }

            var restRequest = new RestRequest("user/{username}/reset-password", Method.Patch)
                .AddParameter("username", request.Name ?? string.Empty, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<string>(restRequest, cancellationToken);
            if (result.IsSuccessful)
            {
                var addResponse = new AddUserResponse
                {
                    Password = result.Data
                };

                var table = CliTableExtensions.GetTable(addResponse);
                return new CliActionResponse(result, table);
            }
            else
            {
                return new CliActionResponse(result);
            }
        }

        [Action("set-password")]
        public static async Task<CliActionResponse> SetUserPassword(CliSetUserPasswordRequest request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("user/{username}/password", Method.Patch)
                .AddParameter("username", request.Name ?? string.Empty, ParameterType.UrlSegment)
                .AddBody(request);

            var result = await RestProxy.Invoke(restRequest, cancellationToken);
            return new CliActionResponse(result);
        }

        [Action("ls")]
        [Action("list")]
        public static async Task<CliActionResponse> GetUsers(CliPagingRequest request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("user", Method.Get)
                .AddQueryPagingParameter(request);

            return await ExecuteTable<PagingResponse<UserRowModel>>(restRequest, CliTableExtensions.GetTable, cancellationToken);
        }

        [NullRequest]
        [Action("remove")]
        [Action("delete")]
        public static async Task<CliActionResponse> RemoveUserById(CliByNameRequest request, CancellationToken cancellationToken = default)
        {
            request ??= new CliByNameRequest();
            var wrapper = await FillGetRequest(request, cancellationToken);
            if (!wrapper.IsSuccessful)
            {
                return new CliActionResponse(wrapper.FailResponse);
            }

            if (!ConfirmAction($"remove user {request.Name}")) { return CliActionResponse.Empty; }

            var restRequest = new RestRequest("user/{username}", Method.Delete)
                .AddParameter("username", request.Name ?? string.Empty, ParameterType.UrlSegment);

            return await Execute(restRequest, cancellationToken);
        }

        [Action("update")]
        public static async Task<CliActionResponse> UpdateUser(CliUpdateEntityByNameRequest request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("user", Method.Patch)
                .AddBody(request);

            return await Execute(restRequest, cancellationToken);
        }

        [Action("join")]
        public static async Task<CliActionResponse> AddGroupToUser(CliUserToGroupRequest request, CancellationToken cancellationToken = default)
        {
            var wrapper = await FillCliUserToGroupRequest1(request, cancellationToken);
            if (!wrapper.IsSuccessful)
            {
                return new CliActionResponse(wrapper.FailResponse);
            }

            var restRequest = new RestRequest("group/{name}/user/{username}", Method.Patch)
               .AddParameter("name", request.GroupName, ParameterType.UrlSegment)
               .AddParameter("username", request.Name ?? string.Empty, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke(restRequest, cancellationToken);
            return new CliActionResponse(result);
        }

        [Action("exclude")]
        public static async Task<CliActionResponse> RemoveUserFromGroup(CliUserToGroupRequest request, CancellationToken cancellationToken = default)
        {
            var wrapper = await FillCliUserToGroupRequest2(request, cancellationToken);
            if (!wrapper.IsSuccessful)
            {
                return new CliActionResponse(wrapper.FailResponse);
            }

            var restRequest = new RestRequest("group/{name}/user/{username}", Method.Delete)
               .AddParameter("name", request.GroupName, ParameterType.UrlSegment)
               .AddParameter("username", request.Name ?? string.Empty, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke(restRequest, cancellationToken);
            return new CliActionResponse(result);
        }

        private static CliAddUserRequest GetCliAddUserRequest()
        {
            const string phone_pattern = "^0[0-9]+$";
            const string email_pattern = @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9]{2,8}(?:[a-z0-9-]*[a-z0-9])?)\Z";

            var result = new CliAddUserRequest
            {
                Username = CollectCliValue("username", true, 2, 50) ?? string.Empty,
                FirstName = CollectCliValue("first name", true, 2, 50) ?? string.Empty,
                LastName = CollectCliValue("last name", false, 2, 50),
                EmailAddress1 = CollectCliValue("email address", false, 5, 250, email_pattern, "is not valid email"),
                PhoneNumber1 = CollectCliValue("phone number", false, 9, 50, phone_pattern, "is not valid phone number (only digits start with 0)")
            };

            return result;
        }

        private static async Task<CliPromptWrapper> FillGetRequest(CliByNameRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.Name))
            {
                var p1 = await CliPromptUtil.Users(cancellationToken);
                if (!p1.IsSuccessful) { return p1; }
                request.Name = p1.Value ?? string.Empty;
            }

            return CliPromptWrapper.Success;
        }

        private static async Task<CliPromptWrapper> FillCliUserToGroupRequest1(CliUserToGroupRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.Name))
            {
                var p1 = await CliPromptUtil.Users(cancellationToken);
                if (!p1.IsSuccessful) { return p1; }
                request.Name = p1.Value ?? string.Empty;
            }

            if (string.IsNullOrEmpty(request.GroupName))
            {
                var p1 = await CliPromptUtil.GroupsWithoutUser(request.Name, cancellationToken);
                if (!p1.IsSuccessful) { return p1; }
                request.GroupName = p1.Value ?? string.Empty;
            }

            return CliPromptWrapper.Success;
        }

        private static async Task<CliPromptWrapper> FillCliUserToGroupRequest2(CliUserToGroupRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.Name))
            {
                var p1 = await CliPromptUtil.Users(cancellationToken);
                if (!p1.IsSuccessful) { return p1; }
                request.Name = p1.Value ?? string.Empty;
            }

            if (string.IsNullOrEmpty(request.GroupName))
            {
                var p1 = await CliPromptUtil.GroupsForUser(request.Name, cancellationToken);
                if (!p1.IsSuccessful) { return p1; }
                request.GroupName = p1.Value ?? string.Empty;
            }

            return CliPromptWrapper.Success;
        }
    }
}