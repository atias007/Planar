using FluentValidation;

namespace Planar.Service.Validation;

public class BaseProcessJobPropertiesValidator : AbstractValidator<BaseProcessJobProperties>
{
    public BaseProcessJobPropertiesValidator()
    {
        RuleFor(e => e.Domain).MaximumLength(100);
        RuleFor(e => e.UserName).MaximumLength(100);
        RuleFor(e => e.Password).MaximumLength(100);
    }
}
