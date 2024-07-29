using FluentValidation;
using System.Runtime.InteropServices;

namespace Planar.Service.Validation;

public class BaseProcessJobPropertiesValidator : AbstractValidator<BaseProcessJobProperties>
{
    public BaseProcessJobPropertiesValidator()
    {
        RuleFor(e => e.Domain).MaximumLength(100);
        RuleFor(e => e.UserName).MaximumLength(100);
        RuleFor(e => e.Password).MaximumLength(100);

        RuleFor(e => e.Domain).Null()
            .When(p => !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            .WithMessage("{PropertyName} must be null when operation system is not windows");

        RuleFor(e => e.Password).Null()
            .When(p => !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            .WithMessage("{PropertyName} must be null when operation system is not windows");
    }
}