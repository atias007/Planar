using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.CliGeneral;
using Planar.CLI.Entities;
using Planar.CLI.Proxy;
using RestSharp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.Actions;

[Module("user", "handle users and groups", Synonyms = "users")]
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
    public static async Task<CliActionResponse> UpdateUser(CliUpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            var p1 = await CliPromptUtil.Users(cancellationToken);
            if (!p1.IsSuccessful) { return new CliActionResponse(p1.FailResponse); }
            request.Username = p1.Value ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(request.Username)) { return CliActionResponse.Empty; }

        var restRequest = new RestRequest("user/{username}", Method.Get)
            .AddParameter("username", request.Username ?? string.Empty, ParameterType.UrlSegment);

        var detailsResponse = await RestProxy.Invoke<UserDetails>(restRequest, cancellationToken);
        var details = detailsResponse.IsSuccessful && detailsResponse.Data != null ? detailsResponse.Data : null;
        if (details == null) { return new CliActionResponse(detailsResponse); }

        FillOptionalString(request, nameof(request.Username), defaultValue: details.Username);

        FillOptionalString(request, nameof(request.FirstName), defaultValue: details.FirstName);
        FillOptionalString(request, nameof(request.LastName), defaultValue: details.LastName);

        FillOptionalString(request, nameof(request.PhoneNumber1), defaultValue: details.PhoneNumber1);
        FillOptionalString(request, nameof(request.PhoneNumber2), defaultValue: details.PhoneNumber2);
        FillOptionalString(request, nameof(request.PhoneNumber3), defaultValue: details.PhoneNumber3);

        FillOptionalString(request, nameof(request.EmailAddress1), defaultValue: details.EmailAddress1);
        FillOptionalString(request, nameof(request.EmailAddress2), defaultValue: details.EmailAddress2);
        FillOptionalString(request, nameof(request.EmailAddress3), defaultValue: details.EmailAddress3);

        FillOptionalString(request, nameof(request.AdditionalField1), defaultValue: details.AdditionalField1);
        FillOptionalString(request, nameof(request.AdditionalField2), defaultValue: details.AdditionalField2);
        FillOptionalString(request, nameof(request.AdditionalField3), defaultValue: details.AdditionalField3);
        FillOptionalString(request, nameof(request.AdditionalField4), defaultValue: details.AdditionalField4);
        FillOptionalString(request, nameof(request.AdditionalField5), defaultValue: details.AdditionalField5);

        var body = new
        {
            request.Username,
            CurrentUsername = details.Username,
            request.FirstName,
            request.LastName,
            request.PhoneNumber1,
            request.PhoneNumber2,
            request.PhoneNumber3,
            request.EmailAddress1,
            request.EmailAddress2,
            request.EmailAddress3,
            request.AdditionalField1,
            request.AdditionalField2,
            request.AdditionalField3,
            request.AdditionalField4,
            request.AdditionalField5
        };

        restRequest = new RestRequest("user", Method.Put)
            .AddBody(body);

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
            Username = CollectCliValue(new CollectCliValueParameters
            {
                Field = "username",
                Required = true,
                MinLength = 2,
                MaxLength = 50
            }) ?? string.Empty,
            FirstName = CollectCliValue(new CollectCliValueParameters
            {
                Field = "first name",
                Required = true,
                MinLength = 2,
                MaxLength = 50
            }) ?? string.Empty,
            LastName = CollectCliValue(new CollectCliValueParameters
            {
                Field = "last name",
                Required = false,
                MinLength = 2,
                MaxLength = 50
            }),
            EmailAddress1 = CollectCliValue(new CollectCliValueParameters
            {
                Field = "email address",
                Required = false,
                MinLength = 5,
                MaxLength = 250,
                Regex = email_pattern,
                RegexErrorMessage = "is not valid email"
            }),
            PhoneNumber1 = CollectCliValue(new CollectCliValueParameters
            {
                Field = "phone number",
                Required = false,
                MinLength = 9,
                MaxLength = 50,
                Regex = phone_pattern,
                RegexErrorMessage = "is not valid phone number (only digits start with 0)"
            })
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