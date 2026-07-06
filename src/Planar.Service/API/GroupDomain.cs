using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.API.Helpers;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.API;

public class GroupDomain(IServiceProvider serviceProvider) : BaseLazyBL<GroupDomain, IGroupData>(serviceProvider)
{
    public static IEnumerable<string> GetAllGroupsRoles()
    {
        return Enum.GetNames<Roles>().Select(r => r.ToLower());
    }

    public async Task<EntityIdResponse> AddGroup(AddGroupRequest request)
    {
        var group = Mapper.Map<Group>(request);
        var groupRoleValue = RoleHelper.GetRoleValue(group.Role) ?? throw new RestValidationException("role", $"role '{request.Role}' is not supported");
        if (AppSettings.Authentication.HasAuthontication && (int)UserRole < groupRoleValue)
        {
            AuditSecuritySafe($"creating a group with name '{group.Name}' and role '{request.Role}' blocked because the current user role is '{UserRole}'", isWarning: true);
            throw new RestForbiddenException();
        }

        group.Role = group.Role.ToLower();
        if (await DataLayer.IsGroupNameExists(request.Name, 0))
        {
            throw new RestConflictException($"group with {nameof(request.Name).ToLower()} '{request.Name}' already exists");
        }

        try
        {
            await DataLayer.AddGroup(group);
        }
        catch (DbUpdateException)
        {
            if (await DataLayer.IsGroupNameExists(request.Name, 0))
            {
                throw new RestConflictException($"group with {nameof(request.Name).ToLower()} '{request.Name}' already exists");
            }

            throw;
        }

        AuditSecuritySafe($"group '{group.Name}' was created with role '{request.Role?.ToLower()}'");
        return new EntityIdResponse(group.Id);
    }

    public async Task AddUserToGroup(string name, string username)
    {
        var groupId = await DataLayer.GetGroupId(name);
        if (groupId == 0) { throw new RestNotFoundException($"group '{name}' could not be found"); }

        var userData = Resolve<IUserData>();
        var userId = await userData.GetUserId(username);
        if (userId == 0) { throw new RestNotFoundException($"user with username '{username}' could not be found"); }

        if (await DataLayer.IsUserExistsInGroup(userId, groupId))
        {
            throw new RestValidationException("username", $"username '{username}' already in group '{name}'");
        }

        var currentUserRole = await Resolve<IUserData>().GetUserRole(userId);
        var targetUserRole = await DataLayer.GetGroupRole(name);
        var targetUserRoleValue = RoleHelper.GetRoleValue(targetUserRole);
        var currentUserRoleValue = RoleHelper.GetRoleValue(currentUserRole);

        if (AppSettings.Authentication.HasAuthontication && targetUserRoleValue > (int)UserRole)
        {
            AuditSecuritySafe($"adding user '{username}' to group '{name}' with role '{(Roles)targetUserRoleValue}' blocked because the current user role is '{UserRole}'", isWarning: true);
            throw new RestForbiddenException();
        }

        await DataLayer.AddUserToGroup(userId, groupId);

        AuditSecuritySafe($"user '{username}' was joined to group '{name}'");
        if (targetUserRoleValue > currentUserRoleValue)
        {
            AuditSecuritySafe($"the user '{username}' elevate its role from '{currentUserRole}' to '{targetUserRole}' by joining group '{name}'", isWarning: true);
        }
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
        ValidateExistingEntity(group, "group");
        var users = await DataLayer.GetUsersInGroup(group!.Id);
        var mapper = Resolve<IMapper>();
        var result = mapper.Map<GroupDetails>(group);
        users.ForEach(result.Users.Add);

        return result;
    }

    public async Task PartialUpdateGroup(UpdateEntityRequestByName request)
    {
        TrimPropertyName(request);
        ForbbidenPartialUpdateProperties(request, $"to update role use: planar-cli group set-role command {request.Name} {request.PropertyValue}", nameof(UpdateGroupRequest.Role));
        ForbbidenPartialUpdateProperties(request, $"to join user to group use: planar-cli group join {request.Name} {request.PropertyValue}", nameof(GroupDetails.Users));

        var group = await DataLayer.GetGroup(request.Name);
        ValidateExistingEntity(group, "group");
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
        var group = ValidateExistingEntity(entity, "group");

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

    public async Task Update(UpdateGroupRequest request)
    {
        var id = await DataLayer.GetGroupId(request.CurrentName);
        if (id == 0) { throw new RestNotFoundException($"group '{request.CurrentName}' could not be found"); }

        var group = Mapper.Map<Group>(request);
        var groupRoleValue = RoleHelper.GetRoleValue(group.Role);
        if (AppSettings.Authentication.HasAuthontication && (int)UserRole < groupRoleValue)
        {
            AuditSecuritySafe($"creating a group with name '{group.Name}' and role '{request.Role}' blocked because the current user role is '{UserRole}'", isWarning: true);
            throw new RestForbiddenException();
        }

        group.Id = id;
        if (await DataLayer.IsGroupNameExists(request.Name, id))
        {
            throw new RestConflictException($"group '{request.Name}' already exists");
        }

        try
        {
            await DataLayer.UpdateGroup(group);
        }
        catch (DbUpdateException)
        {
            if (await DataLayer.IsGroupNameExists(request.Name, id))
            {
                throw new RestConflictException($"group '{request.Name}' already exists");
            }

            throw;
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