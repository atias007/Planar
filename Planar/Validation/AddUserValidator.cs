using FluentValidation;
using Planar.Service.Model;

namespace Planar.Validation
{
    public class AddUserValidator : AbstractValidator<User>
    {
        public AddUserValidator()
        {
            RuleFor(u => u.Username).NotEmpty().Length(2, 50);
            RuleFor(u => u.FirstName).NotEmpty().Length(2, 50);
            RuleFor(u => u.LastName).NotEmpty().Length(2, 50);
            RuleFor(u => u.EmailAddress1).Length(5, 250).EmailAddress();
            RuleFor(u => u.EmailAddress2).Length(5, 250).EmailAddress();
            RuleFor(u => u.EmailAddress3).Length(5, 250).EmailAddress();
            RuleFor(u => u.PhoneNumber1).Length(9, 50).OnlyDigits();
            RuleFor(u => u.PhoneNumber2).Length(9, 50).OnlyDigits();
            RuleFor(u => u.PhoneNumber3).Length(9, 50).OnlyDigits();
            RuleFor(u => u.Reference1).MaximumLength(500);
            RuleFor(u => u.Reference2).MaximumLength(500);
            RuleFor(u => u.Reference3).MaximumLength(500);
            RuleFor(u => u.Reference4).MaximumLength(500);
            RuleFor(u => u.Reference5).MaximumLength(500);
        }
    }
}