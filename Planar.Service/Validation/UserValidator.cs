using FluentValidation;
using Planar.Service.Model;

namespace Planar.Service.Validation
{
    public class UserValidator : AbstractValidator<User>
    {
        public UserValidator()
        {
            RuleFor(u => u.Username).NotEmpty().Length(2, 50);
            RuleFor(u => u.FirstName).NotEmpty().Length(2, 50);
            RuleFor(u => u.LastName).NotEmpty().Length(2, 50);
            RuleFor(u => u.EmailAddress1).Length(5, 250).Must(ValidationUtil.IsValidEmail).WithMessage("Invalid email address");
            RuleFor(u => u.EmailAddress2).Length(5, 250).Must(ValidationUtil.IsValidEmail).WithMessage("Invalid email address");
            RuleFor(u => u.EmailAddress3).Length(5, 250).Must(ValidationUtil.IsValidEmail).WithMessage("Invalid email address");
            RuleFor(u => u.PhoneNumber1).Length(9, 50).Must(ValidationUtil.IsOnlyDigits).WithMessage("Invalid numeric value");
            RuleFor(u => u.PhoneNumber2).Length(9, 50).Must(ValidationUtil.IsOnlyDigits).WithMessage("Invalid numeric value");
            RuleFor(u => u.PhoneNumber3).Length(9, 50).Must(ValidationUtil.IsOnlyDigits).WithMessage("Invalid numeric value");
            RuleFor(u => u.Reference1).Length(0, 500);
            RuleFor(u => u.Reference2).Length(0, 500);
            RuleFor(u => u.Reference3).Length(0, 500);
            RuleFor(u => u.Reference4).Length(0, 500);
            RuleFor(u => u.Reference5).Length(0, 500);
        }
    }
}