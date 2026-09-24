using FluentValidation;
using Microsoft.AspNetCore.Http;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.API.Helpers;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Planar.Service.General.Hash;
using Planar.Service.General.Password;
using Planar.Service.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.API;

public class UserDomain(IServiceProvider serviceProvider) : BaseLazyBL<UserDomain, IUserData>(serviceProvider)
{
    private const string kind = "user";
    private const string kind_pass = "user password";

    public async Task<ApplyResponse> Apply(HttpContext httpContext)
    {
        var yamls = await GetApplyYamls(httpContext, kind, kind_pass);
        var result = await Apply(yamls, httpContext.RequestAborted);
        return result;
    }

    public async Task<AddUserResponse> Add(AddUserRequest request)
    {
        if (await DataLayer.IsUsernameExists(request.Username))
        {
            throw new RestConflictException($"user with username '{request.Username}' already exists");
        }

        return await AddUserInner(request);
    }

    private async Task<AddUserResponse> AddUserInner(AddUserRequest request)
    {
        var hash = GeneratePassword();
        var user = Mapper.Map<User>(request);
        user.Password = hash.Hash;
        user.Salt = hash.Salt;

        _ = await DataLayer.AddUser(user);

        AuditSecuritySafe($"user '{user.Username}' was created");

        var response = new AddUserResponse
        {
            Password = hash.Value
        };

        return response;
    }

    public async Task Delete(string username)
    {
        var count = await DataLayer.RemoveUser(username);
        if (count == 0)
        {
            throw new RestNotFoundException($"user with username '{username}' could not be found");
        }

        AuditSecuritySafe($"user '{username}' was deleted");
    }

    public async Task<UserDetails> Get(string username)
    {
        var user = await DataLayer.GetUser(username);
        ValidateExistingEntity(user, "user");
        var groups = await DataLayer.GetGroupsForUser(user!.Id);
        var result = Mapper.Map<UserDetails>(user);
        groups.ForEach(g => result.Groups.Add(g.ToString()));
        var role = await DataLayer.GetUserRole(username);
        result.Role = role ?? RoleHelper.DefaultRole;
        return result;
    }

    public async Task<PagingResponse<UserRowModel>> GetAll(IPagingRequest request)
    {
        var query = DataLayer.GetUsers();
        var result = await query.ProjectToWithPagingAsyc<User, UserRowModel>(Mapper, request);
        return result;
    }

    public async Task<string> GetRole(string username)
    {
        if (!await DataLayer.IsUsernameExists(username))
        {
            throw new RestNotFoundException($"user with username '{username}' could not be found");
        }

        var role = await DataLayer.GetUserRole(username) ?? RoleHelper.DefaultRole;
        return role;
    }

    public async Task PartialUpdate(UpdateEntityRequestByName request)
    {
        TrimPropertyName(request);
        ForbiddenPartialUpdateProperties(request, $"to join user to group use: planar-cli user join {request.Name} {request.PropertyValue}", nameof(UserDetails.Groups));
        ForbiddenPartialUpdateProperties(request, $"to update user password use: planar-cli user set-password  {request.Name} {request.PropertyValue}", nameof(User.Password));
        ForbiddenPartialUpdateProperties(request, "to update user role join the user to group which has an appropriate role", nameof(UserDetails.Role));

        var user = await DataLayer.GetUser(request.Name);
        ValidateExistingEntity(user, "user");
        var updateUser = Mapper.Map<UpdateUserRequest>(user);
        updateUser.CurrentUsername = request.Name;
        var validator = Resolve<IValidator<UpdateUserRequest>>();
        await SetEntityProperties(updateUser, request, validator);
        await Update(updateUser);
    }

    public async Task<string> ResetPassword(string username)
    {
        await EnsureCanManage(username);
        var existsUser = await DataLayer.GetUser(username, withTracking: true);
        ValidateExistingEntity(existsUser, "user");
        if (existsUser == null) { return string.Empty; }
        var hash = GeneratePassword();
        existsUser.Password = hash.Hash;
        existsUser.Salt = hash.Salt;
        await DataLayer.SaveChangesAsync();

        AuditSecuritySafe($"password for user '{username}' was reset", isWarning: true);

        return hash.Value;
    }

    public async Task<bool> SetPassword(string username, SetPasswordRequest request)
    {
        var existsUser = await DataLayer.GetUser(username, withTracking: true);
        ValidateExistingEntity(existsUser, "user");
        if (existsUser == null) { return false; }

        return await SetPasswordInner(username, request.Password, existsUser);
    }

    private async Task<bool> SetPasswordInner(string username, string password, User existsUser)
    {
        var verify = HashUtil.VerifyHash(password, existsUser.Password, existsUser.Salt);
        if (verify) { return false; }

        var hash = HashUtil.CreateHash(password);
        existsUser.Password = hash.Hash;
        existsUser.Salt = hash.Salt;
        await DataLayer.SaveChangesAsync();

        AuditSecuritySafe($"password for user '{username}' was changed", isWarning: true);
        return true;
    }

    public async Task Update(UpdateUserRequest request)
    {
        var current = await DataLayer.GetUser(request.CurrentUsername, withTracking: true)
            ?? throw new RestNotFoundException($"user with username '{request.CurrentUsername}' is not exists");

        if (await DataLayer.IsUsernameExists(request.Username, request.CurrentUsername))
        {
            throw new RestConflictException($"user with username '{request.Username}' already exists");
        }

        await UpdateInner(request, current);
    }

    private async Task<int> UpdateInner(AddUserRequest request, User current)
    {
        Mapper.Map(request, current);
        var count = await DataLayer.SaveChangesAsync();
        if (count > 0)
        {
            AuditSecuritySafe($"user '{current.Username}' was updated");
        }

        return count;
    }

    internal async Task<ApplyResponse> Apply(IEnumerable<KeyValuePair<string, string>> yamls, CancellationToken cancellationToken)
    {
        var user_yaml = yamls.Where(y => y.Key == kind);
        var pass_yaml = yamls.Where(y => y.Key == kind_pass);

        // Convert to list of ApplyUserRequest & ApplyPasswordRequest
        var user_requests = await GetApplyEntities<ApplyUserRequest>(user_yaml, kind, cancellationToken, withValidation: true);
        var password_requests = await GetApplyEntities<ApplyPasswordRequest>(pass_yaml, kind_pass, cancellationToken, withValidation: true);

        // Validation
        ValidateDuplicateRequests(user_requests);
        ValidateDuplicateRequests(password_requests);

        // Apply changes
        var user_response = await ApplyChanges(user_requests);
        var password_response = await ApplyChanges(password_requests);

        var response = ApplyResponse.Merge(user_response, password_response);
        return response;
    }

    private async Task<ApplyResponse> ApplyChanges(IReadOnlyCollection<ApplyUserRequest> requests)
    {
        var response = new ApplyResponse();
        if (requests.Count == 0) { return response; }
        if (requests.Count == 1)
        {
            var request = requests.First();
            var result = await ApplyInner(request);
            response.AddItem(result);
            return response;
        }

        foreach (var request in requests)
        {
            try
            {
                var result = await ApplyInner(request);
                response.AddItem(result);
            }
            catch (Exception ex)
            {
                response.AddItem(new ApplyResponseItem(request.Username ?? string.Empty, ApplyAction.Error, $"fail to handle user '{request.Username}'. {ex.Message}", Manifest.User, request.Source));
            }
        }

        return response;
    }

    private async Task<ApplyResponse> ApplyChanges(IReadOnlyCollection<ApplyPasswordRequest> requests)
    {
        var response = new ApplyResponse();
        if (requests.Count == 0) { return response; }
        if (requests.Count == 1)
        {
            var request = requests.First();
            var result = await ApplyInner(request);
            response.AddItem(result);
            return response;
        }

        foreach (var request in requests)
        {
            try
            {
                var result = await ApplyInner(request);
                response.AddItem(result);
            }
            catch (Exception ex)
            {
                response.AddItem(new ApplyResponseItem(request.Username ?? string.Empty, ApplyAction.Error, $"fail to handle password for user '{request.Username}'. {ex.Message}", Manifest.UserPassword, request.Source));
            }
        }

        return response;
    }

    private async Task<ApplyResponseItem> ApplyInner(ApplyUserRequest request)
    {
        request.Username = request.Username?.Trim();
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new RestValidationException("invalid request", "username is required");
        }

        var exists = await DataLayer.GetUser(request.Username, withTracking: true);
        if (exists == null)
        {
            await AddUserInner(request);
            return new ApplyResponseItem(request.Username, ApplyAction.Add, $"user '{request.Username}' was added", Manifest.User, request.Source);
        }
        else
        {
            var count = await UpdateInner(request, exists);
            var message = count > 0 ? $"user '{request.Username}' was updated" : $"user '{request.Username}' was not changed";
            var action = count > 0 ? ApplyAction.Update : ApplyAction.Unchanged;
            return new ApplyResponseItem(request.Username, action, message, Manifest.User, request.Source);
        }
    }

    private async Task<ApplyResponseItem> ApplyInner(ApplyPasswordRequest request)
    {
        request.Username = request.Username?.Trim();
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new RestValidationException("invalid request", "username is required");
        }

        var exists = await DataLayer.GetUser(request.Username, withTracking: true);
        if (exists == null)
        {
            return new ApplyResponseItem(request.Username, ApplyAction.Error, $"user '{request.Username}' does not exist", Manifest.UserPassword, request.Source);
        }
        else
        {
            var success = await SetPasswordInner(request.Username, request.Password, exists);
            var message = success ? $"password for user '{request.Username}' was updated" : $"password for user '{request.Username}' was not changed";
            var action = success ? ApplyAction.Update : ApplyAction.Unchanged;
            return new ApplyResponseItem(request.Username, action, message, Manifest.UserPassword, request.Source);
        }
    }

    private static void ValidateDuplicateRequests(IEnumerable<ApplyUserRequest> requests)
    {
        var query = requests
            .GroupBy(r => r.Username)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .FirstOrDefault();

        if (query != null)
        {
            throw new RestValidationException("duplicate request", $"duplicate user request for username '{query}'");
        }
    }

    private static void ValidateDuplicateRequests(IEnumerable<ApplyPasswordRequest> requests)
    {
        var query = requests
            .GroupBy(r => r.Username)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .FirstOrDefault();

        if (query != null)
        {
            throw new RestValidationException("duplicate request", $"duplicate user password request for username '{query}'");
        }
    }

    private static HashEntity GeneratePassword()
    {
        var password = PasswordGenerator.GeneratePassword(
           new PasswordGeneratorBuilder()
           .IncludeLowercase()
           .IncludeNumeric()
           .IncludeSpecial()
           .IncludeUppercase()
           .WithLength(12)
           .Build());

        var hash = HashUtil.CreateHash(password);
        return hash;
    }

    private async Task EnsureCanManage(string username)
    {
        if (!AppSettings.Authentication.HasAuthontication) { return; }
        var targetRole = RoleHelper.GetRoleValue(await DataLayer.GetUserRole(username)) ?? 0;
        if (targetRole > (int)UserRole)
        {
            AuditSecuritySafe($"trying to set/reset password by user '{username}' blocked because the target role outranks current user '{UserRole}'", isWarning: true);
            throw new RestForbiddenException();
        }
    }
}