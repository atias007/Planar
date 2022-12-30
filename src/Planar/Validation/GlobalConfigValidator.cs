using FluentValidation;
using Planar.Service.Model;

namespace Planar.Validation
{
    public class GlobalConfigValidator : AbstractValidator<GlobalConfig>
    {
        public GlobalConfigValidator()
        {
            RuleFor(c => c.Key).NotEmpty().MaximumLength(50);
            RuleFor(c => c.Value).NotEmpty().MaximumLength(1000);
            RuleFor(c => c.Type).NotEmpty().MaximumLength(10);
        }
    }
}