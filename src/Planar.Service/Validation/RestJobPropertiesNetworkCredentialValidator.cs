using FluentValidation;

namespace Planar.Service.Validation;

internal class RestJobPropertiesNetworkCredentialValidator : AbstractValidator<RestJobPropertiesNetworkCredential>
{
    public RestJobPropertiesNetworkCredentialValidator()
    {
        RuleFor(r => r.Password)
            .NotEmpty()
            .WithMessage("'password' must not be empty")
            .MaximumLength(100)
            .WithMessage(e => $"the length of 'password' must be 100 characters or fewer. You entered {e.Password?.Length ?? 0} characters");

        RuleFor(r => r.Username)
            .NotEmpty()
            .WithMessage("'username' must not be empty")
            .MaximumLength(100)
            .WithMessage(e => $"the length of 'username' must be 100 characters or fewer. You entered {e.Username?.Length ?? 0} characters");

        RuleFor(r => r.Domain)
            .MaximumLength(100)
            .WithMessage(e => $"the length of 'domain' must be 100 characters or fewer. You entered {e.Domain?.Length ?? 0} characters");
    }
}