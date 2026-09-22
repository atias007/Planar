using FluentValidation;

namespace Planar.Service.Validation;

internal class RestJobBasicAuthenticationValidator : AbstractValidator<RestJobBasicAuthentication>
{
    public RestJobBasicAuthenticationValidator()
    {
        RuleFor(r => r.Password)
            .NotEmpty()
            .WithMessage("'password' must not be empty");

        RuleFor(r => r.Password)
            .MaximumLength(1000)
            .WithMessage(e => $"the length of 'password' must be 1000 characters or fewer. You entered {e.Password?.Length ?? 0} characters");

        RuleFor(r => r.Username)
            .NotEmpty()
            .WithMessage("'username' must not be empty");

        RuleFor(r => r.Username)
            .MaximumLength(1000)
            .WithMessage(e => $"the length of 'username' must be 1000 characters or fewer. You entered {e.Username?.Length ?? 0} characters");
    }
}