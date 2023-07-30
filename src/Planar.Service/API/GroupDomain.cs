using AutoMapper;
using FluentValidation;
using Planar.API.Common.Entities;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public class GroupDomain : BaseBL<GroupDomain, GroupData>
    {
        public GroupDomain(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<EntityIdResponse> AddGroup(AddGroupRequest request)
        {
            if (await DataLayer.IsGroupNameExists(request.Name, 0))
            {
                throw new RestConflictException($"group with {nameof(request.Name).ToLower()} '{request.Name}' already exists");
            }

            var group = Mapper.Map<Group>(request);
            await DataLayer.AddGroup(group);
            return new EntityIdResponse(group.Id);
        }

        public async Task<GroupDetails> GetGroupByName(string name)
        {
            var group = await DataLayer.GetGroup(name);
            ValidateExistingEntity(group, "group");
            var users = await DataLayer.GetUsersInGroup(group!.Id);
            var mapper = Resolve<IMapper>();
            var result = mapper.Map<GroupDetails>(group);
            users.ForEach(u => result.Users.Add(u.ToString()));

            return result;
        }

        public async Task<PagingResponse<GroupInfo>> GetAllGroups(IPagingRequest request)
        {
            return await DataLayer.GetGroups(request);
        }

        public static IEnumerable<string> GetAllGroupsRoles()
        {
            return Enum.GetNames<Roles>().Select(r => r.ToLower());
        }

        public async Task DeleteGroup(string name)
        {
            var id = await DataLayer.GetGroupId(name);
            if (id == 0) { throw new RestNotFoundException($"group '{name}' could not be found"); }

            await ValidateMonitorForGroup(id);
            await ValidateUsersForGroup(id);

            var count = await DataLayer.RemoveGroup(id);
            if (count < 1)
            {
                throw new RestNotFoundException($"group '{name}' could not be found");
            }
        }

        private async Task ValidateMonitorForGroup(int groupId)
        {
            var hasMonitor = await DataLayer.IsGroupHasMonitors(groupId);
            if (hasMonitor)
            {
                throw new RestValidationException("id", "group has one or more monitor item/s and can not be deleted");
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

        public async Task PartialUpdateGroup(UpdateEntityRequestByName request)
        {
            ForbbidenPartialUpdateProperties(request, "to update role use 'group set-role' command", nameof(UpdateGroupRequest.Role), nameof(UpdateGroupRequest.RoleId));
            ForbbidenPartialUpdateProperties(request, "to join user to group use 'group join'", nameof(GroupDetails.Users));
            ForbbidenPartialUpdateProperties(request, null, nameof(GroupDetails.Users));

            var group = await DataLayer.GetGroup(request.Name);
            ValidateExistingEntity(group, "group");
            var updateGroup = Mapper.Map<UpdateGroupRequest>(group);
            updateGroup.CurrentName = request.Name;
            updateGroup.Role = null;
            var validator = Resolve<IValidator<UpdateGroupRequest>>();
            await SetEntityProperties(updateGroup, request, validator);
            await Update(updateGroup);
        }

        public async Task Update(UpdateGroupRequest request)
        {
            var id = await DataLayer.GetGroupId(request.CurrentName);
            if (id == 0) { throw new RestNotFoundException($"group '{request.CurrentName}' could not be found"); }

            if (await DataLayer.IsGroupNameExists(request.Name, id))
            {
                throw new RestConflictException($"group '{request.Name}' already exists");
            }

            var group = Mapper.Map<Group>(request);
            await DataLayer.UpdateGroup(group);
        }

        public async Task AddUserToGroup(string name, string username)
        {
            var groupId = await DataLayer.GetGroupId(name);
            if (groupId == 0) { throw new RestNotFoundException($"group '{name}' could not be found"); }

            var userData = Resolve<UserData>();
            var userId = await userData.GetUserId(username);
            if (userId == 0) { throw new RestNotFoundException($"user with username '{username}' could not be found"); }

            if (await DataLayer.IsUserExistsInGroup(userId, groupId))
            {
                throw new RestValidationException("username", $"username '{username}' already in group '{name}'");
            }

            var currentUserRole = await Resolve<UserData>().GetUserRole(userId);
            var targetUserRole = await DataLayer.GetGroupRole(name);

            await DataLayer.AddUserToGroup(userId, groupId);

            if (targetUserRole > currentUserRole)
            {
                var currentUserRoleTitle = ((Roles)currentUserRole).ToString().ToLower();
                var targetUserRoleTitle = ((Roles)targetUserRole).ToString().ToLower();
                AuditSecuritySafe($"the user '{username}' elevate its role from '{currentUserRoleTitle}' to '{targetUserRoleTitle}' by joining group '{name}'", isWarning: true);
            }
        }

        public async Task SetRoleToGroup(string name, string role)
        {
            var entity = await DataLayer.GetGroup(name);
            var group = ValidateExistingEntity(entity, "group");

            if (!Enum.TryParse<Roles>(role, true, out var roleEnum))
            {
                throw new RestNotFoundException($"role '{role?.ToLower()}' could not be found");
            }

            if ((int)roleEnum == group.RoleId)
            {
                throw new RestNotFoundException($"group '{name}' already has role '{role?.ToLower()}'");
            }

            var isWarning = (int)roleEnum > group.RoleId;
            if (isWarning)
            {
                AuditSecuritySafe($"the group '{name}' elevate its role from '{group.Role.Name?.ToLower()}' to '{role}'", isWarning: true);
            }
            else
            {
                AuditSecuritySafe($"the group '{name}' bring down its role from '{group.Role.Name?.ToLower()}' to '{role}'", isWarning: false);
            }

            await DataLayer.SetRoleToGroup(group.Id, (int)roleEnum);
        }

        public async Task RemoveUserFromGroup(string name, string username)
        {
            var groupId = await DataLayer.GetGroupId(name);
            if (groupId == 0) { throw new RestNotFoundException($"group '{name}' could not be found"); }

            var userData = Resolve<UserData>();
            var userId = await userData.GetUserId(username);
            if (userId == 0) { throw new RestNotFoundException($"user with username '{username}' could not be found"); }

            if (!await DataLayer.IsUserExistsInGroup(userId, groupId))
            {
                throw new RestValidationException("username", $"user with username '{username}' does not exist in group '{name}'");
            }

            await DataLayer.RemoveUserFromGroup(userId, groupId);
        }
    }
}