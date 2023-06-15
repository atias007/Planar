using FluentValidation;
using Planar.API.Common.Entities;
using System;

namespace Planar.Service.Validation
{
    public class ConcurrentExecutionRequestValidator : AbstractValidator<ConcurrentExecutionRequest>
    {
        public ConcurrentExecutionRequestValidator()
        {
            RuleFor(c => c.Server).MaximumLength(100);
            RuleFor(c => c.InstanceId).MaximumLength(100);
            RuleFor(c => c.ToDate).GreaterThan(r => r.FromDate).LessThan(DateTime.Now);
            RuleFor(c => c.FromDate).LessThan(DateTime.Now);
        }
    }
}