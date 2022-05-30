using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Validation
{
    public class AddUserRequestValidator : AbstractValidator<AddUserRequest>
    {
        public AddUserRequestValidator()
        {
            RuleFor(r => r.Username).NotEmpty().Length(2, 50);
            RuleFor(r => r.FirstName).NotEmpty().Length(2, 50);
            RuleFor(r => r.LastName).Length(2, 50);
            RuleFor(r => r.Email).Length(5, 250).EmailAddress();
            RuleFor(r => r.PhoneNumber).Length(9, 50).OnlyDigits();
        }
    }
}