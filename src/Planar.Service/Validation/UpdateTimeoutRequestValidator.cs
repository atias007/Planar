using FluentValidation;
using Planar.API.Common.Entities;
using System;

namespace Planar.Service.Validation
{
    public class UpdateTimeoutRequestValidator : AbstractValidator<UpdateTimeoutRequest>
    {
        public UpdateTimeoutRequestValidator()
        {
            Include(new JobOrTriggerKeyValidator());
            RuleFor(x => x.Timeout)
                .NotZero()
                .LessThanOrEqualTo(TimeSpan.FromDays(1))
                .GreaterThanOrEqualTo(TimeSpan.FromMinutes(1));
        }
    }
}