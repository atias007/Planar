using FluentValidation;
using Planar.API.Common.Entities;
using Planar.Service.API;
using Planar.Service.Validation;

namespace Planar.Service
{
    public class AddUserRequestValidator : AbstractValidator<AddUserRequest>
    {
        public AddUserRequestValidator(UserDomain bl)
        {
            RuleFor(r => r.Username).NotEmpty().Length(2, 50)
                .Must(n => !bl.IsUsernameExists(n).Result).WithMessage("username '{PropertyValue}' already exists");
            RuleFor(r => r.FirstName).NotEmpty().Length(2, 50);
            RuleFor(r => r.LastName).Length(2, 50);
            RuleFor(r => r.EmailAddress1).Length(5, 250).EmailAddress();
            RuleFor(r => r.EmailAddress2).Length(5, 250).EmailAddress();
            RuleFor(r => r.EmailAddress3).Length(5, 250).EmailAddress();
            RuleFor(r => r.PhoneNumber1).Length(9, 50).OnlyDigits();
            RuleFor(r => r.PhoneNumber2).Length(9, 50).OnlyDigits();
            RuleFor(r => r.PhoneNumber3).Length(9, 50).OnlyDigits();
            RuleFor(u => u.Reference1).MaximumLength(500);
            RuleFor(u => u.Reference2).MaximumLength(500);
            RuleFor(u => u.Reference3).MaximumLength(500);
            RuleFor(u => u.Reference4).MaximumLength(500);
            RuleFor(u => u.Reference5).MaximumLength(500);
        }
    }
}