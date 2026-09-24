using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.API.Helpers;
using Planar.Service.Audit;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Planar.Service.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.API;

public class GroupDomain(IServiceProvider serviceProvider) : BaseLazyBL<GroupDomain, IGroupData>(serviceProvider)
{
    private const string c_group = "group";
    private const string kind = c_group;

    public async Task<ApplyResponse> Apply(HttpContext httpContext)
    {
        var yamls = await GetApplyYamls(httpContext, kind);
        var result = await Apply(yamls, httpContext.RequestAborted);
        return result;
    }

    public static IEnumerable<string> GetAllGroupsRoles()
    {
        return Enum.GetNames<Roles>().Select(r => r.ToLower());
    }

    public async Task<EntityIdResponse> AddGroup(AddGroupRequest request)
    {
        if (await DataLayer.IsGroupNameExists(request.Name, 0))
        {
            throw new RestConflictException($"group with {nameof(request.Name).ToLower()} '{request.Name}' already exists");
        }

        var group = BuildNewGroup(request);
        return await SaveNewGroup(group);
    }

    private Group BuildNewGroup(AddGroupRequest request)
    {
        var group = Mapper.Map<Group>(request);
        var groupRoleValue = RoleHelper.GetRoleValue(group.Role) ?? throw new RestValidationException("role", $"role '{request.Role}' is not supported");
        if (AppSettings.Authentication.HasAuthontication && (int)UserRole < groupRoleValue)
        {
            AuditSecuritySafe($"creating a group with name '{group.Name}' and role '{request.Role}' blocked because the current user role is '{UserRole}'", isWarning: true);
            throw new RestForbiddenException();
        }

        group.Role = group.Role.ToLower();
        return group;
    }

    private async Task<EntityIdResponse> SaveNewGroup(Group group)
    {
        try
        {
            await DataLayer.AddGroup(group);
        }
        catch (DbUpdateException)
        {
            if (await DataLayer.IsGroupNameExists(group.Name, 0))
            {
                throw new RestConflictException($"group with {nameof(group.Name).ToLower()} '{group.Name}' already exists");
            }

            throw;
        }

        AuditSecuritySafe($"group '{group.Name}' was created with role '{group.Role?.ToLower()}'");
        return new EntityIdResponse(group.Id);
    }

    public async Task AddUserToGroup(string groupName, string username)
    {
        var groupId = await DataLayer.GetGroupId(groupName);
        if (groupId == 0) { throw new RestNotFoundException($"group '{groupName}' could not be found"); }

        var userData = Resolve<IUserData>();
        var userId = await userData.GetUserId(username);
        if (userId == 0) { throw new RestNotFoundException($"user with username '{username}' could not be found"); }

        if (await DataLayer.IsUserExistsInGroup(userId, groupId))
        {
            throw new RestValidationException("username", $"username '{username}' already in group '{groupName}'");
        }

        var audits = await ValidatePermissionForAddUserToGroup(groupName, username, userId);
        await DataLayer.AddUserToGroup(userId, groupId);

        foreach (var audit in audits)
        {
            AuditSecuritySafe(audit);
        }
    }

    private async Task<IReadOnlyCollection<SecurityMessage>> ValidatePermissionForAddUserToGroup(string groupName, string username, int userId)
    {
        var result = new List<SecurityMessage>();
        var currentUserRole = await Resolve<IUserData>().GetUserRole(userId);
        var targetUserRole = await DataLayer.GetGroupRole(groupName);
        var targetUserRoleValue = RoleHelper.GetRoleValue(targetUserRole);
        var currentUserRoleValue = RoleHelper.GetRoleValue(currentUserRole);

        if (AppSettings.Authentication.HasAuthontication && targetUserRoleValue > (int)UserRole)
        {
            AuditSecuritySafe($"adding user '{username}' to group '{groupName}' with role '{(Roles)targetUserRoleValue}' blocked because the current user role is '{UserRole}'", isWarning: true);
            throw new RestForbiddenException();
        }

        var msg1 = GetAuditSecurityMessage($"user '{username}' was joined to group '{groupName}'");
        if (msg1 != null) { result.Add(msg1); }
        if (targetUserRoleValue > currentUserRoleValue)
        {
            var msg2 = GetAuditSecurityMessage($"the user '{username}' elevate its role from '{currentUserRole}' to '{targetUserRole}' by joining group '{groupName}'", isWarning: true);
            if (msg2 != null) { result.Add(msg2); }
        }

        return result;
    }

    public async Task DeleteGroup(string name)
    {
        var id = await DataLayer.GetGroupId(name);
        if (id == 0) { throw new RestNotFoundException($"group '{name}' could not be found"); }

        await ValidateMonitorForGroup(id);
        await ValidateUsersForGroup(id);

        var count = await DataLayer.RemoveGroup(id);
        if (count == 0)
        {
            throw new RestNotFoundException($"group '{name}' could not be found");
        }

        AuditSecuritySafe($"group '{name}' was deleted");
    }

    public async Task<PagingResponse<GroupInfo>> GetAllGroups(IPagingRequest request)
    {
        return await DataLayer.GetGroups(request);
    }

    public async Task<GroupDetails> GetGroupByName(string name)
    {
        var group = await DataLayer.GetGroup(name);
        ValidateExistingEntity(group, c_group);
        var users = await DataLayer.GetUsersInGroup(group!.Id);
        var mapper = Resolve<IMapper>();
        var result = mapper.Map<GroupDetails>(group);
        users.ForEach(result.Users.Add);

        return result;
    }

    public async Task PartialUpdateGroup(UpdateEntityRequestByName request)
    {
        TrimPropertyName(request);
        ForbiddenPartialUpdateProperties(request, $"to update role use: planar-cli group set-role command {request.Name} {request.PropertyValue}", nameof(UpdateGroupRequest.Role));
        ForbiddenPartialUpdateProperties(request, $"to join user to group use: planar-cli group join {request.Name} {request.PropertyValue}", nameof(GroupDetails.Users));

        var group = await DataLayer.GetGroup(request.Name);
        ValidateExistingEntity(group, c_group);
        var updateGroup = Mapper.Map<UpdateGroupRequest>(group);
        updateGroup.CurrentName = request.Name;
        var validator = Resolve<IValidator<UpdateGroupRequest>>();
        await SetEntityProperties(updateGroup, request, validator);
        await Update(updateGroup);
    }

    public async Task RemoveUserFromGroup(string name, string username)
    {
        var groupId = await DataLayer.GetGroupId(name);
        if (groupId == 0) { throw new RestNotFoundException($"group '{name}' could not be found"); }

        var userData = Resolve<IUserData>();
        var userId = await userData.GetUserId(username);
        if (userId == 0) { throw new RestNotFoundException($"user with username '{username}' could not be found"); }

        if (!await DataLayer.IsUserExistsInGroup(userId, groupId))
        {
            throw new RestValidationException("username", $"user with username '{username}' does not exist in group '{name}'");
        }

        await DataLayer.RemoveUserFromGroup(userId, groupId);

        AuditSecuritySafe($"user '{username}' was removed from group '{name}'");
    }

    public async Task SetRoleToGroup(string name, string role)
    {
        var entity = await DataLayer.GetGroup(name);
        var group = ValidateExistingEntity(entity, c_group);

        var clearGroupRole = RoleHelper.CleanRole(group.Role);
        var cleanTargetRole = RoleHelper.CleanRole(role);

        if (clearGroupRole == cleanTargetRole)
        {
            throw new RestNotFoundException($"group '{name}' already has role '{cleanTargetRole}'");
        }

        var currentUserRoleValue = RoleHelper.GetRoleValue(UserRole);
        var targetRoleValue = RoleHelper.GetRoleValue(cleanTargetRole) ?? throw new RestValidationException("role", $"role '{role}' is not supported");

        if (AppSettings.Authentication.HasAuthontication && targetRoleValue > currentUserRoleValue)
        {
            AuditSecuritySafe($"setting role '{role}' to group '{name}' blocked because the current user role is '{UserRole}'", isWarning: true);
            throw new RestForbiddenException();
        }

        await DataLayer.SetRoleToGroup(group.Id, cleanTargetRole);

        var groupRoleValue = RoleHelper.GetRoleValue(clearGroupRole);
        var isWarning = targetRoleValue > groupRoleValue;
        if (isWarning)
        {
            AuditSecuritySafe($"the group '{name}' elevate its role from '{group.Role}' to '{role}'", isWarning: true);
        }
        else
        {
            AuditSecuritySafe($"the group '{name}' bring down its role from '{group.Role}' to '{role}'", isWarning: false);
        }
    }

    public async Task<int> Update(UpdateGroupRequest request)
    {
        var exists = await DataLayer.GetGroupWithTrackChanges(request.CurrentName) ??
            throw new RestNotFoundException($"group '{request.CurrentName}' could not be found");

        if (request.IsNameChanged && await DataLayer.IsGroupNameExists(request.Name, exists.Id))
        {
            throw new RestConflictException($"group '{request.Name}' already exists");
        }

        await UpdateInner(request, exists);

        try
        {
            return await DataLayer.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            if (await DataLayer.IsGroupNameExists(request.Name, exists.Id))
            {
                throw new RestConflictException($"group '{request.Name}' already exists");
            }

            throw;
        }
    }

    private async Task UpdateInner(AddGroupRequest request, Group exists)
    {
        var group = Mapper.Map(request, exists);
        var groupRoleValue = RoleHelper.GetRoleValue(group.Role);
        if (AppSettings.Authentication.HasAuthontication && (int)UserRole < groupRoleValue)
        {
            AuditSecuritySafe($"creating a group with name '{group.Name}' and role '{request.Role}' blocked because the current user role is '{UserRole}'", isWarning: true);
            throw new RestForbiddenException();
        }
    }

    internal async Task<ApplyResponse> Apply(IEnumerable<KeyValuePair<string, string>> yamls, CancellationToken cancellationToken)
    {
        // Convert to list of ApplyGroupRequest
        var requests = await GetApplyEntities<ApplyGroupRequest>(yamls, kind, cancellationToken);

        // Validation
        ValidateDuplicateRequests(requests);
        await ValidateUserNamesExists(requests);

        // Apply changes
        var response = await ApplyChanges(requests);

        // Save changes
        await DataLayer.SaveChangesAsync();

        return response;
    }

    private async Task<ApplyResponse> ApplyChanges(IReadOnlyCollection<ApplyGroupRequest> requests)
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
                response.AddItem(new ApplyResponseItem(request.Name, ApplyAction.Error, $"fail to handle group: {ex.Message}", Manifest.Group, request.Source));
            }
        }

        return response;
    }

    private async Task<ApplyResponseItem> ApplyInner(ApplyGroupRequest request)
    {
        var all_audits = new List<SecurityMessage>();
        request.Role = request.Role?.ToLower();

        // inline function
        async Task AddUserToGroup(Group group, string username)
        {
            var userData = Resolve<IUserData>();
            var user = await userData.GetUser(username, withTracking: true);
            if (user == null) { return; }
            var audits = await ValidatePermissionForAddUserToGroup(request.Name, user.Username, user.Id);
            group.Users.Add(user);
            all_audits.AddRange(audits);
        }

        // inline function
        void PublishAudits()
        {
            foreach (var item in all_audits)
            {
                AuditSecuritySafe(item);
            }
        }

        // get group
        var dal = Resolve<IGroupData>();
        var exists_group = await dal.GetGroupWithTrackChanges(request.Name);

        if (exists_group == null)
        {
            // new group
            var group = BuildNewGroup(request);
            foreach (var username in request.Users)
            {
                await AddUserToGroup(group, username);
            }

            await SaveNewGroup(group);
            PublishAudits();
            return new ApplyResponseItem(request.Name, ApplyAction.Add, $"add new group {request.Name}", Manifest.Group, request.Source);
        }
        else
        {
            // update group
            await UpdateInner(request, exists_group);

            // add new users
            var count = 0;
            foreach (var username in request.Users)
            {
                var exists = exists_group.Users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
                if (exists == null)
                {
                    await AddUserToGroup(exists_group, username);
                    count += 1;
                }
            }

            // remove users
            var removed = new List<User>();
            foreach (var user in exists_group.Users)
            {
                var exists = request.Users.FirstOrDefault(u => u.Equals(user.Username, StringComparison.OrdinalIgnoreCase));
                if(exists == null)
                {
                    removed.Add(user);
                }
            }

            removed.ForEach(r => exists_group.Users.Remove(r));

            // save changes
            count += await dal.SaveChangesAsync();
            if (count == 0)
            {
                return new ApplyResponseItem(request.Name, ApplyAction.Unchanged, $"group {request.Name} is unchanged", Manifest.Group, request.Source);
            }

            PublishAudits();
            return new ApplyResponseItem(request.Name, ApplyAction.Update, $"update group {request.Name}", Manifest.Group, request.Source);
        }
    }

    private async Task ValidateUserNamesExists(IEnumerable<ApplyGroupRequest> requests)
    {
        var users = requests.SelectMany(r => r.Users);
        await ValidateUserNamesExistsInner(users);
    }

    private async Task ValidateUserNamesExistsInner(IEnumerable<string> users)
    {
        var userDal = Resolve<IUserData>();
        var list = new List<string>();
        var groups = users.Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var name in groups)
        {
            var exists = await userDal.IsUsernameExists(name);
            if (!exists) { list.Add($"'{name}'"); }
        }

        if (list.Count > 0)
        {
            var names = string.Join(",", list);
            throw new RestValidationException("Usernames", $"user: {names} could not be found");
        }
    }

    private static void ValidateDuplicateRequests(IEnumerable<ApplyGroupRequest> requests)
    {
        var query = requests
            .GroupBy(r => r.Name)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .FirstOrDefault();

        if (query != null)
        {
            throw new RestValidationException("duplicate request", $"duplicate group request for group name '{query}'");
        }

        foreach (var item in requests)
        {
            query = item.Users
            .GroupBy(r => r)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .FirstOrDefault();

            if (query != null)
            {
                throw new RestValidationException("duplicate username", $"duplicate user '{query}' at group name '{item.Name}'");
            }
        }
    }

    private async Task ValidateMonitorForGroup(int groupId)
    {
        var hasMonitor = await DataLayer.IsGroupHasMonitors(groupId);
        if (hasMonitor)
        {
            throw new RestValidationException("id", "group related to one or more monitor item/s and can not be deleted");
        }
    }

    private async Task ValidateUsersForGroup(int groupId)
    {
        var hasMonitor = await DataLayer.IsGroupHasUsers(groupId);
        if (hasMonitor)
        {
            throw new RestValidationException("id", $"group has one or more user/s and can not be deleted");
        }
    }
}