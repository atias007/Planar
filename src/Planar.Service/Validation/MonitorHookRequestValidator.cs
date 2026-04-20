using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation;

public class MonitorHookRequestValidator : AbstractValidator<MonitorHookRequest>
{
    public MonitorHookRequestValidator()
    {
        RuleFor(r => r.MonitorId).GreaterThan(0);

        RuleFor(r => r.Hook).NotEmpty();

        RuleFor(r => r.Hook)
            .Must(ValidationUtil.IsHookExists)
            .When(r => !string.IsNullOrWhiteSpace(r.Hook))
            .WithMessage("{PropertyName} '{PropertyValue}' does not exist");
    }
}
