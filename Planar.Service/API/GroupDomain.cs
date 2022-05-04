using FluentValidation;
using Microsoft.Extensions.Logging;
using Planar.Service.API.Validation;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.Model;
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
            return await DataLayer.GetGroupWithUsers(id);
        }

        public async Task<object> GetGroups()
        {
            return await DataLayer.GetGroups();
        }

        public async Task DeleteGroup(int id)
        {
            var group = new Group { Id = id };
            await DataLayer.RemoveGroup(group);
        }

        public async Task UpdateGroup(int id, UpdateEntityRecord request)
        {
            await new UpdateEntityRecordValidator().ValidateAndThrowAsync(request);

            if (await DataLayer.GetGroup(id) is not Group existsGroup)
            {
                throw new PlanarValidationException($"Group with id {id} could not be found");
            }

            var properties = typeof(Group).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var prop = properties.FirstOrDefault(p => p.Name == request.PropertyName);
            if (prop == null)
            {
                throw new PlanarValidationException($"PropertyName '{request.PropertyName}' could not be found in Group entity");
            }

            try
            {
                var stringValue = request.PropertyValue;
                if (stringValue.ToLower() == "[null]") { stringValue = null; }
                var value = Convert.ChangeType(stringValue, prop.PropertyType);
                prop.SetValue(existsGroup, value);
            }
            catch (Exception ex)
            {
                throw new PlanarValidationException($"PropertyValue '{request.PropertyValue}' could not be set to PropertyName '{request.PropertyName}' ({ex.Message})");
            }

            await new GroupValidator().ValidateAndThrowAsync(existsGroup);

            await DataLayer.UpdateGroup(existsGroup);
        }

        public async Task AddUserToGroup(int groupId, UserToGroupRecord request)
        {
            if (await DataLayer.IsUserExists(request.UserId) == false) { throw new PlanarValidationException($"UserId {request.UserId} is not exists"); }
            if (await DataLayer.IsGroupExists(groupId) == false) { throw new PlanarValidationException($"GroupId {groupId} is not exists"); }
            if (await DataLayer.IsUserExistsInGroup(request.UserId, groupId)) { throw new PlanarValidationException($"UserId {request.UserId} already in GroupId {groupId}"); }

            await DataLayer.AddUserToGroup(request.UserId, groupId);
        }

        public async Task RemoveUserFromGroup(int groupId, int userId)
        {
            if (await DataLayer.IsUserExists(userId) == false) { throw new PlanarValidationException($"UserId {userId} is not exists"); }
            if (await DataLayer.IsGroupExists(groupId) == false) { throw new PlanarValidationException($"GroupId {groupId} is not exists"); }
            if (await DataLayer.IsUserExistsInGroup(userId, groupId) == false) { throw new PlanarValidationException($"UserId {userId} is not exists in GroupId {groupId}"); }

            await DataLayer.RemoveUserFromGroup(userId, groupId);
        }
    }
}