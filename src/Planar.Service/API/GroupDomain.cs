using AutoMapper;
using Azure.Core;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Planar.API.Common.Entities;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.Model;
using System;
using System.Collections.Generic;
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

        public async Task<List<GroupInfo>> GetAllGroups()
        {
            return await DataLayer.GetGroups();
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
            if (string.Equals(request.PropertyName, "role", StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(request.PropertyName, "roleid", StringComparison.OrdinalIgnoreCase))
            {
                throw new RestValidationException("property name", "role property can not be updated. to update role use 'group set-role' command");
            }

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

            await DataLayer.AddUserToGroup(userId, groupId);
        }

        public async Task SetRoleToGroup(string name, string role)
        {
            var groupId = await DataLayer.GetGroupId(name);
            if (groupId == 0) { throw new RestNotFoundException($"group '{name}' could not be found"); }

            if (!Enum.TryParse<Roles>(role, true, out var roleEnum))
            {
                throw new RestNotFoundException($"role '{role}' could not be found");
            }

            await DataLayer.SetRoleToGroup(groupId, (int)roleEnum);
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