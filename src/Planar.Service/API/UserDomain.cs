using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Planar.API.Common.Entities;
using Planar.Service.Exceptions;
using Planar.Service.General.Hash;
using Planar.Service.General.Password;
using Planar.Service.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public class UserDomain : BaseBL<UserDomain>
    {
        public UserDomain(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<AddUserResponse> Add(AddUserRequest request)
        {
            var hash = GeneratePassword();
            var user = Mapper.Map<User>(request);
            user.Password = hash.Hash;
            user.Salt = hash.Salt;

            var result = await DataLayer.AddUser(user);
            var response = new AddUserResponse
            {
                Id = result.Id,
                Password = hash.Value
            };

            return response;
        }

        public async Task<UserDetails> Get(int id)
        {
            var user = await DataLayer.GetUser(id);
            ValidateExistingEntity(user);
            var groups = await DataLayer.GetGroupsForUser(id);
            var mapper = Resolve<IMapper>();
            var result = mapper.Map<UserDetails>(user);
            groups.ForEach(g => result.Groups.Add(g.ToString()));
            return result;
        }

        public async Task<List<UserRow>> GetAll()
        {
            return await DataLayer.GetUsers();
        }

        public async Task Delete(int id)
        {
            var user = new User { Id = id };

            try
            {
                await DataLayer.RemoveUser(user);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new RestNotFoundException($"{nameof(Group)} entity could not be found");
            }
        }

        public async Task PartialUpdate(int id, UpdateEntityRecord request)
        {
            ValidateIdConflict(id, request.Id);

            // ValidateForbiddenUpdateProperties(request, "Id", "UsersToGroups", "Groups", "Password", "Salt", "Username");
            var user = await DataLayer.GetUser(request.Id);
            var updateUser = Mapper.Map<UpdateUserRequest>(user);
            await SetEntityProperties(updateUser, request, new AddUserRequestValidator(this));
            await Update(updateUser);
        }

        public async Task Update(UpdateUserRequest request)
        {
            var exists = await DataLayer.IsUserExists(request.Id);
            if (!exists)
            {
                throw new RestNotFoundException($"user with id {request} is not exists");
            }

            var user = Mapper.Map<User>(request);
            await DataLayer.UpdateUser(user);
        }

        public async Task<string> ResetPassword(int id)
        {
            var existsUser = await DataLayer.GetUser(id);
            ValidateExistingEntity(existsUser);
            var hash = GeneratePassword();
            existsUser.Password = hash.Hash;
            existsUser.Salt = hash.Salt;
            return hash.Value;
        }

        public async Task<bool> IsUsernameExists(string username)
        {
            var result = await DataLayer.IsUsernameExists(username);
            return result;
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
    }
}