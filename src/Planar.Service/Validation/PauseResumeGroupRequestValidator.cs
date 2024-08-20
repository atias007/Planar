using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation;

internal class PauseResumeGroupRequestValidator : AbstractValidator<PauseResumeGroupRequest>
{
    public PauseResumeGroupRequestValidator()
    {
        RuleFor(RuleFor => RuleFor.Name).NotEmpty().MaximumLength(50);
    }
}