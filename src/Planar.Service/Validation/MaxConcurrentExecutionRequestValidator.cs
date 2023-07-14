using FluentValidation;
using Planar.API.Common.Entities;
using System;

namespace Planar.Service.Validation
{
    public class MaxConcurrentExecutionRequestValidator : AbstractValidator<MaxConcurrentExecutionRequest>
    {
        public MaxConcurrentExecutionRequestValidator()
        {
            var maxHour = DateTime.Now.AddHours(-1);
            RuleFor(c => c.ToDate).LessThan(maxHour);
            RuleFor(c => c.FromDate)
                .LessThanOrEqualTo(r => r.ToDate.GetValueOrDefault().AddHours(-1))
                .When(r => r.ToDate.HasValue);
            RuleFor(c => c.FromDate).LessThan(maxHour);
        }
    }
}