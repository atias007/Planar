using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation;

public class JobWaitRequestValidator : AbstractValidator<JobWaitRequest>
{
    public JobWaitRequestValidator()
    {
        RuleFor(x => x.Id).MaximumLength(101);
        RuleFor(x => x.Group).MaximumLength(50);

        RuleFor(x => x.Id)
            .Empty()
            .When(x => !string.IsNullOrWhiteSpace(x.Group))
            .WithMessage("{PropertyName} must be empty when 'Group' has value");

        RuleFor(x => x.Group)
            .Empty()
            .When(x => !string.IsNullOrWhiteSpace(x.Id))
            .WithMessage("{PropertyName} must be empty when 'Id' has value");
    }
}