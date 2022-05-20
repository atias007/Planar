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

        [Action("get/{id}")]
        public static async Task<CliActionResponse> GetUserById(int id)
        {
            var restRequest = new RestRequest("user/{id}", Method.Get)
                .AddParameter("id", id, ParameterType.UrlSegment);

            return await ExecuteEntity<CliUser>(restRequest);
        }

        [Action("ls")]
        [Action("list")]
        public static async Task<CliActionResponse> GetUsers()
        {
            var restRequest = new RestRequest("user", Method.Get);
            return await ExecuteEntity<List<UserRowDetails>>(restRequest, CliTableExtensions.GetTable);
        }

        [Action("remove")]
        [Action("delete")]
        public static async Task<CliActionResponse> RemoveUserById(int id)
        {
            var restRequest = new RestRequest("user/{id}", Method.Delete)
                .AddParameter("id", id, ParameterType.UrlSegment);

            return await ExecuteEntity(restRequest);
        }

        [Action("update")]
        public static async Task<CliActionResponse> UpdateUser(CliUpdateEntityRequest request)
        {
            var restRequest = new RestRequest("user/{id}", Method.Patch)
                .AddParameter("id", request.Id, ParameterType.UrlSegment)
                .AddBody(request);

            return await ExecuteEntity(restRequest);
        }

        [Action("password")]
        public static async Task<CliActionResponse> GetUserPassword(int id)
        {
            var restRequest = new RestRequest("user/{id}/password", Method.Get)
                .AddParameter("id", id, ParameterType.UrlSegment);

            return await ExecuteEntity<string>(restRequest);
        }
    }
}