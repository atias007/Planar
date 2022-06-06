using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.Model;
using Planar.Service.Validation;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public class GroupDomain : BaseBL<GroupDomain>
    {
        public GroupDomain(ILogger<GroupDomain> logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        }

        public async Task<int> AddGroup(UpsertGroupRecord request)
        {
            var group = new Group
            {
                Id = request.Id,
                Name = request.Name
            };

            await DataLayer.AddGroup(group);
            return group.Id;
        }

        public async Task<object> GetGroupById(int id)
        {
            var result = await DataLayer.GetGroupWithUsers(id);
            ValidateExistingEntity(result);
            return result;
        }

        public async Task<object> GetGroups()
        {
            return await DataLayer.GetGroups();
        }

        public async Task DeleteGroup(int id)
        {
            var group = new Group { Id = id };
            try
            {
                await DataLayer.RemoveGroup(group);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new RestNotFoundException();
            }
        }

        public async Task UpdateGroup(int id, UpdateEntityRecord request)
        {
            ValidateIdConflict(id, request.Id);
            ValidateForbiddenUpdateProperties(request, "Id", "MonitorActions", "UsersToGroups", "Users");
            var existsGroup = await DataLayer.GetGroup(id);
            ValidateExistingEntity(existsGroup);
            await UpdateEntity(existsGroup, request, new GroupValidator());
            await DataLayer.UpdateGroup(existsGroup);
        }

        public async Task AddUserToGroup(int groupId, UserToGroupRecord request)
        {
            if (await DataLayer.IsUserExists(request.UserId) == false) { throw new RestValidationException("user id", $"user id {request.UserId} does not exist"); }
            if (await DataLayer.IsGroupExists(groupId) == false) { throw new RestValidationException("group id", $"group id {groupId} does not exist"); }
            if (await DataLayer.IsUserExistsInGroup(request.UserId, groupId)) { throw new RestValidationException("user id", $"user id {request.UserId} already in group id {groupId}"); }

            await DataLayer.AddUserToGroup(request.UserId, groupId);
        }

        public async Task RemoveUserFromGroup(int groupId, int userId)
        {
            if (await DataLayer.IsUserExists(userId) == false) { throw new RestValidationException("user id", $"user id {userId} does not exist"); }
            if (await DataLayer.IsGroupExists(groupId) == false) { throw new RestValidationException("group id", $"group id {groupId} does not exist"); }
            if (await DataLayer.IsUserExistsInGroup(userId, groupId) == false) { throw new RestValidationException("user id", $"user id {userId} does not exist in group id {groupId}"); }

            await DataLayer.RemoveUserFromGroup(userId, groupId);
        }
    }
}