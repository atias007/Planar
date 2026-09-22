using FluentValidation;

namespace Planar.Service.Validation;

internal class RestJobJwtAuthenticationValidator : AbstractValidator<RestJobJwtAuthentication>
{
    public RestJobJwtAuthenticationValidator()
    {
        RuleFor(r => r.Token)
            .NotEmpty()
            .WithMessage("'token' must not be empty");

        RuleFor(r => r.Token)
            .MaximumLength(1000)
            .WithMessage(e => $"the length of 'token' must be 1000 characters or fewer. You entered {e.Token?.Length ?? 0} characters");
    }
}