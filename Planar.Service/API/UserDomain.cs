using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.Exceptions;
using Planar.Service.General.Password;
using Planar.Service.Model;
using Planar.Service.Validation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public class UserDomain : BaseBL<UserDomain>
    {
        public UserDomain(ILogger<UserDomain> logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        }

        public async Task<AddUserResponse> Add(AddUserRequest request)
        {
            var password = PasswordGenerator.GeneratePassword(
               new PasswordGeneratorBuilder()
               .IncludeLowercase()
               .IncludeNumeric()
               .IncludeSpecial()
               .IncludeUppercase()
               .WithLength(12)
               .Build());

            var user = new User
            {
                Username = request.Username,
                EmailAddress1 = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber1 = request.PhoneNumber,
                Password = password
            };

            await new UserValidator().ValidateAndThrowAsync(user);
            var result = await DataLayer.AddUser(user);
            var response = new AddUserResponse
            {
                Id = result.Id,
                Password = result.Password
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
                throw new RestNotFoundException();
            }
        }

        public async Task Update(int id, UpdateEntityRecord request)
        {
            ValidateIdConflict(id, request.Id);
            ValidateForbiddenUpdateProperties(request, "Id", "UsersToGroups", "Groups");
            var existsUser = await DataLayer.GetUser(request.Id);
            ValidateExistingEntity(existsUser);
            await UpdateEntity(existsUser, request, new UserValidator());
            await DataLayer.UpdateUser(existsUser);
        }

        public async Task<string> GetPassword(int id)
        {
            var password = await DataLayer.GetPassword(id);
            return password;
        }
    }
}