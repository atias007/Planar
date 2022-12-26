using FluentValidation;
using Planar.API.Common.Entities;
using Planar.Service.Data;

namespace Planar.Service
{
    public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
    {
        public UpdateUserRequestValidator(IValidator<AddUserRequest> addValidator, UserData userData)
        {
            Include(addValidator);
            RuleFor(e => e.Id).GreaterThan(0);
        }
    }
}