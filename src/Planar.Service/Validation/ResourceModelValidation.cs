using FluentValidation;
using Planar.Service.Model;

namespace Planar.Service.Validation;

public class ResourceModelValidation : AbstractValidator<ResourceModel>
{
    public ResourceModelValidation()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(100);
        RuleFor(r => r.Value).NotEmpty().MaximumLength(1_000_000);
    }
}