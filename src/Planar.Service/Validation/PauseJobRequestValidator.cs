using FluentValidation;
using Planar.API.Common.Entities;
using System;

namespace Planar.Service.Validation;

public class PauseJobRequestValidator : AbstractValidator<PauseResumeJobRequest>
{
    public PauseJobRequestValidator()
    {
        Include(new JobOrTriggerKeyValidator());
        RuleFor(x => x.AutoResumeDate).GreaterThan(DateTime.Now).When(x => x.AutoResumeDate != null);
    }
}