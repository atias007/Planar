using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("user")]
    public class UserCliActions : BaseCliAction<UserCliActions>
    {
        [Action("add")]
        public static async Task<CliActionResponse> AddUser()
        {
            var prm = CollectDataFromCli<AddUserRequest>();
            var restRequest = new RestRequest("user", Method.Post)
                .AddBody(prm);

            return await ExecuteEntity<AddUserResponse>(restRequest);
        }

        [Action("get")]
        public static async Task<CliActionResponse> GetUserById(CliGetByIdRequest request)
        {
            var restRequest = new RestRequest("user/{id}", Method.Get)
                .AddParameter("id", request.Id, ParameterType.UrlSegment);

            return await ExecuteEntity<UserDetails>(restRequest);
        }

        [Action("ls")]
        [Action("list")]
        public static async Task<CliActionResponse> GetUsers()
        {
            var restRequest = new RestRequest("user", Method.Get);
            return await ExecuteTable<List<UserRowDetails>>(restRequest, CliTableExtensions.GetTable);
        }

        [Action("remove")]
        [Action("delete")]
        public static async Task<CliActionResponse> RemoveUserById(CliGetByIdRequest request)
        {
            var restRequest = new RestRequest("user/{id}", Method.Delete)
                .AddParameter("id", request.Id, ParameterType.UrlSegment);

            return await Execute(restRequest);
        }

        [Action("update")]
        public static async Task<CliActionResponse> UpdateUser(CliUpdateEntityRequest request)
        {
            var restRequest = new RestRequest("user/{id}", Method.Patch)
                .AddParameter("id", request.Id, ParameterType.UrlSegment)
                .AddBody(request);

            return await Execute(restRequest);
        }

        [Action("password")]
        public static async Task<CliActionResponse> GetUserPassword(CliGetByIdRequest request)
        {
            var restRequest = new RestRequest("user/{id}/password", Method.Get)
                .AddParameter("id", request.Id, ParameterType.UrlSegment);

            return await ExecuteEntity<string>(restRequest);
        }
    }
}