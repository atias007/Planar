using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Validation
{
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(u => u.Username).Length(2, 50);
            RuleFor(x => x.Password).Length(2, 50);
        }
    }
}