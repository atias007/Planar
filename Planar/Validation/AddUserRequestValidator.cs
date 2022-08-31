using FluentValidation;
using Planar.API.Common.Entities;
using Planar.Service.API;
using Planar.Service.Validation;

namespace Planar.Validation
{
    public class AddUserRequestValidator : AbstractValidator<AddUserRequest>
    {
        public AddUserRequestValidator(UserDomain bl)
        {
            RuleFor(r => r.Username).NotEmpty().Length(2, 50).Must(n => bl.IsUsernameExists(n).Result == false).WithMessage("username '{PropertyValue}' already exists");
            RuleFor(r => r.FirstName).NotEmpty().Length(2, 50);
            RuleFor(r => r.LastName).Length(2, 50);
            RuleFor(r => r.Email).Length(5, 250).EmailAddress();
            RuleFor(r => r.PhoneNumber).Length(9, 50).OnlyDigits();
        }
    }
}