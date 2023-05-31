using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service
{
    public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
    {
        public UpdateUserRequestValidator(IValidator<AddUserRequest> addValidator)
        {
            Include(addValidator);
            RuleFor(e => e.CurrentUsername).NotEmpty().Length(2, 50);
        }
    }
}