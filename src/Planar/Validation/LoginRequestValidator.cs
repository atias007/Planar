using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Validation
{
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(u => u.Username).NotEmpty().Length(2, 50);
            RuleFor(x => x.Password).NotEmpty().Length(2, 50);
        }
    }
}