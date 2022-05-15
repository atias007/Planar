using FluentValidation;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.API.Validation;
using Planar.Service.Exceptions;
using Planar.Service.General.Password;
using Planar.Service.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

            var result = await DataLayer.AddUser(user);
            var response = new AddUserResponse
            {
                Id = result.Id,
                Password = result.Password
            };

            return response;
        }

        public async Task<User> Get(int id)
        {
            return await DataLayer.GetUser(id);
        }

        public async Task<List<UserRow>> GetAll()
        {
            return await DataLayer.GetUsers();
        }

        public async Task Delete(int id)
        {
            var user = new User { Id = id };
            await DataLayer.RemoveUser(user);
        }

        public async Task Update(int id, UpdateEntityRecord request)
        {
            if (id != request.Id)
            {
                throw new PlanarValidationException($"Conflict id value. (from routing: {id}, from body {request.Id}");
            }

            await new UpdateEntityRecordValidator().ValidateAndThrowAsync(request);

            if ((await DataLayer.GetUser(request.Id)) is not User existsUser)
            {
                throw new PlanarValidationException($"User with id {request.Id} could not be found");
            }

            var properties = typeof(User).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var prop = properties.FirstOrDefault(p => p.Name == request.PropertyName);
            if (prop == null)
            {
                throw new PlanarValidationException($"PropertyName '{request.PropertyName}' could not be found in User entity");
            }

            try
            {
                var stringValue = request.PropertyValue;
                if (stringValue.ToLower() == "[null]") { stringValue = null; }
                var value = Convert.ChangeType(stringValue, prop.PropertyType);
                prop.SetValue(existsUser, value);
            }
            catch (Exception ex)
            {
                throw new PlanarValidationException($"PropertyValue '{request.PropertyValue}' could not be set to PropertyName '{request.PropertyName}' ({ex.Message})");
            }

            await new UpdateUserValidator().ValidateAndThrowAsync(existsUser);

            await DataLayer.UpdateUser(existsUser);
        }

        public async Task<string> GetPassword(int id)
        {
            var password = await DataLayer.GetPassword(id);
            return password;
        }
    }
}