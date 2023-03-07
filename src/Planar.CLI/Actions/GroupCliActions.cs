using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("group", "Actions to handle groups")]
    public class GroupCliActions : BaseCliAction<GroupCliActions>
    {
        [Action("add")]
        public static async Task<CliActionResponse> Add(CliAddGroupRequest request)
        {
            var restRequest = new RestRequest("group", Method.Post)
                .AddBody(request);
            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("get")]
        public static async Task<CliActionResponse> GetById(CliGetByIdRequest request)
        {
            var restRequest = new RestRequest("group/{id}", Method.Get)
                .AddParameter("id", request.Id, ParameterType.UrlSegment);

            return await ExecuteEntity<GroupDetails>(restRequest);
        }

        [Action("ls")]
        [Action("list")]
        public static async Task<CliActionResponse> Get()
        {
            var restRequest = new RestRequest("group", Method.Get);
            var result = await RestProxy.Invoke<List<GroupInfo>>(restRequest);

            if (result.IsSuccessful)
            {
                var table = CliTableExtensions.GetTable(result.Data);
                return new CliActionResponse(result, table);
            }

            return new CliActionResponse(result);
        }

        [Action("remove")]
        [Action("delete")]
        public static async Task<CliActionResponse> RemoveById(CliGetByIdRequest request)
        {
            if (!ConfirmAction($"remove group id {request.Id}")) { return CliActionResponse.Empty; }

            var restRequest = new RestRequest("group/{id}", Method.Delete)
                .AddParameter("id", request.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("update")]
        public static async Task<CliActionResponse> Update(CliUpdateEntityRequest request)
        {
            var restRequest = new RestRequest("group", Method.Patch)
               .AddBody(request);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("join")]
        public static async Task<CliActionResponse> AddUserToGroup(CliUserToGroupRequest request)
        {
            var restRequest = new RestRequest("group/{id}/user/{userId}", Method.Put)
               .AddParameter("id", request.GroupId, ParameterType.UrlSegment)
               .AddParameter("userId", request.UserId, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("exclude")]
        public static async Task<CliActionResponse> RemoveUserFromGroup(CliUserToGroupRequest request)
        {
            var restRequest = new RestRequest("group/{id}/user/{userId}", Method.Delete)
               .AddParameter("id", request.GroupId, ParameterType.UrlSegment)
               .AddParameter("userId", request.UserId, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }
    }
}