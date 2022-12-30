using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Planar.API.Common.Entities;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.Model;
using System;
using System.Collections.Generic;
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

        public async Task<GroupDetails> GetGroupById(int id)
        {
            var group = await DataLayer.GetGroup(id);
            ValidateExistingEntity(group, "group");
            var users = await DataLayer.GetUsersInGroup(id);
            var mapper = Resolve<IMapper>();
            var result = mapper.Map<GroupDetails>(group);
            users.ForEach(u => result.Users.Add(u.ToString()));

            return result;
        }

        public async Task<List<GroupInfo>> GetAllGroups()
        {
            return await DataLayer.GetGroups();
        }

        public async Task DeleteGroup(int id)
        {
            var exists = await DataLayer.IsGroupExists(id);
            if (!exists)
            {
                throw new RestNotFoundException($"{nameof(Group).ToLower()} with id {id} could not be found");
            }

            await ValidateMonitorForGroup(id);
            await ValidateUsersForGroup(id);

            var group = new Group { Id = id };
            try
            {
                await DataLayer.RemoveGroup(group);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new RestNotFoundException($"{nameof(Group).ToLower()} with id {id} could not be found");
            }
        }

        private async Task ValidateMonitorForGroup(int groupId)
        {
            var hasMonitor = await DataLayer.IsGroupHasMonitors(groupId);
            if (hasMonitor)
            {
                throw new RestValidationException("id", $"group id {groupId}  has one or more monitor item/s and can not be deleted");
            }
        }

        private async Task ValidateUsersForGroup(int groupId)
        {
            var hasMonitor = await DataLayer.IsGroupHasUsers(groupId);
            if (hasMonitor)
            {
                throw new RestValidationException("id", $"group id {groupId} has one or more user/s and can not be deleted");
            }
        }

        public async Task PartialUpdateGroup(UpdateEntityRecord request)
        {
            var group = await DataLayer.GetGroup(request.Id);
            ValidateExistingEntity(group, "monitor");
            var updateGroup = Mapper.Map<UpdateGroupRequest>(group);
            var validator = Resolve<IValidator<UpdateGroupRequest>>();
            await SetEntityProperties(updateGroup, request, validator);
            await Update(updateGroup);
        }

        public async Task Update(UpdateGroupRequest request)
        {
            var exists = await DataLayer.IsGroupExists(request.Id);
            if (!exists)
            {
                throw new RestNotFoundException($"group with id {request.Id} is not exists");
            }

            if (await DataLayer.IsGroupNameExists(request.Name, request.Id))
            {
                throw new RestConflictException($"group with {nameof(request.Name).ToLower()} '{request.Name}' already exists");
            }

            var group = Mapper.Map<Group>(request);
            await DataLayer.UpdateGroup(group);
        }

        public async Task AddUserToGroup(int groupId, int userId)
        {
            if (!await Resolve<UserData>().IsUserExists(userId)) { throw new RestValidationException("user id", $"user id {userId} does not exist"); }
            if (!await DataLayer.IsGroupExists(groupId)) { throw new RestNotFoundException($"group id {groupId} does not exist"); }
            if (await DataLayer.IsUserExistsInGroup(userId, groupId)) { throw new RestValidationException("user id", $"user id {userId} already in group id {groupId}"); }

            await DataLayer.AddUserToGroup(userId, groupId);
        }

        public async Task RemoveUserFromGroup(int groupId, int userId)
        {
            if (!await Resolve<UserData>().IsUserExists(userId)) { throw new RestValidationException("user id", $"user id {userId} does not exist"); }
            if (!await DataLayer.IsGroupExists(groupId)) { throw new RestNotFoundException($"group id {groupId} does not exist"); }
            if (!await DataLayer.IsUserExistsInGroup(userId, groupId)) { throw new RestValidationException("user id", $"user id {userId} does not exist in group id {groupId}"); }

            await DataLayer.RemoveUserFromGroup(userId, groupId);
        }
    }
}