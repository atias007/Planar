using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation;

public class GlobalConfigDataUpdateValidator : AbstractValidator<GlobalConfigModelUpdateRequest>
{
    public GlobalConfigDataUpdateValidator()
    {
        RuleFor(f => f.Key).NotEmpty().MinimumLength(3).MaximumLength(50);
        RuleFor(f => f.Value).MaximumLength(4000);
        RuleFor(f => f.SourceUrl).MaximumLength(1000).IsUri();

        RuleFor(f => f.Value)
           .NotEmpty()
           .When(f => string.IsNullOrWhiteSpace(f.SourceUrl))
           .WithMessage(f => "{PropertyName} must have value when SourceUrl is empty");

        RuleFor(f => f.Value)
           .Empty()
           .When(f => !string.IsNullOrWhiteSpace(f.SourceUrl))
           .WithMessage(f => "{PropertyName} must be empty when SourceUrl is provided");
    }
}