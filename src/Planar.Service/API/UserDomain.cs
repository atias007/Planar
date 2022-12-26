using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Planar.API.Common.Entities;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.General.Hash;
using Planar.Service.General.Password;
using Planar.Service.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public class UserDomain : BaseBL<UserDomain, UserData>
    {
        public UserDomain(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<AddUserResponse> Add(AddUserRequest request)
        {
            if (await DataLayer.IsUsernameExists(request.Username, 0))
            {
                throw new RestConflictException($"{nameof(request.Username)} '{request.Username}' already exists");
            }

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
            ValidateExistingEntity(user, "user");
            var groups = await DataLayer.GetGroupsForUser(id);
            var result = Mapper.Map<UserDetails>(user);
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

        public async Task PartialUpdate(UpdateEntityRecord request)
        {
            var user = await DataLayer.GetUser(request.Id);
            var updateUser = Mapper.Map<UpdateUserRequest>(user);
            var validator = Resolve<IValidator<UpdateUserRequest>>();
            await SetEntityProperties(updateUser, request, validator);
            await Update(updateUser);
        }

        public async Task Update(UpdateUserRequest request)
        {
            if (await DataLayer.IsUsernameExists(request.Username, request.Id))
            {
                throw new RestConflictException($"{nameof(request.Username)} '{request.Username}' already exists");
            }

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
            var existsUser = await DataLayer.GetUser(id, true);
            ValidateExistingEntity(existsUser, "user");
            var hash = GeneratePassword();
            existsUser.Password = hash.Hash;
            existsUser.Salt = hash.Salt;
            await DataLayer.SaveChangesAsync();
            return hash.Value;
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