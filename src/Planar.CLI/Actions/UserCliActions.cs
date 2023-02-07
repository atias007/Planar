using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.CliGeneral;
using Planar.CLI.Entities;
using RestSharp;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("user")]
    public class UserCliActions : BaseCliAction<UserCliActions>
    {
        [Action("add")]
        [NullRequest]
        public static async Task<CliActionResponse> AddUser(CliAddUserRequest request)
        {
            request ??= GetCliAddUserRequest();

            var restRequest = new RestRequest("user", Method.Post)
                .AddBody(request);

            return await ExecuteTable<AddUserResponse>(restRequest, CliTableExtensions.GetTable);
        }

        [Action("get")]
        public static async Task<CliActionResponse> GetUserById(CliGetByIdRequest request)
        {
            var restRequest = new RestRequest("user/{id}", Method.Get)
                .AddParameter("id", request.Id, ParameterType.UrlSegment);

            return await ExecuteEntity<UserDetails>(restRequest);
        }

        [Action("reset-password")]
        public static async Task<CliActionResponse> GetUserPassword(CliGetByIdRequest request)
        {
            var restRequest = new RestRequest("user/{id}/resetpassword", Method.Patch)
                .AddParameter("id", request.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<string>(restRequest);
            if (result.IsSuccessful)
            {
                var addResponse = new AddUserResponse
                {
                    Id = request.Id,
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
            if (!ConfirmAction($"remove user id {request.Id}")) { return CliActionResponse.Empty; }

            var restRequest = new RestRequest("user/{id}", Method.Delete)
                .AddParameter("id", request.Id, ParameterType.UrlSegment);

            return await Execute(restRequest);
        }

        [Action("update")]
        public static async Task<CliActionResponse> UpdateUser(CliUpdateEntityRequest request)
        {
            var restRequest = new RestRequest("user", Method.Patch)
                .AddBody(request);

            return await Execute(restRequest);
        }

        private static CliAddUserRequest GetCliAddUserRequest()
        {
            const string phone_pattern = "^[0-9]+$";
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
    }
}