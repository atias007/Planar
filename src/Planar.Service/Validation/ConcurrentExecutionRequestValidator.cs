using FluentValidation;
using Planar.API.Common.Entities;
using System;

namespace Planar.Service.Validation
{
    public class ConcurrentExecutionRequestValidator : AbstractValidator<ConcurrentExecutionRequest>
    {
        public ConcurrentExecutionRequestValidator()
        {
            var maxHour = DateTime.Now.AddHours(-1);
            RuleFor(c => c.FromDate)
                .LessThanOrEqualTo(r => r.ToDate.GetValueOrDefault().AddHours(-1))
                .When(r => r.ToDate.HasValue);
            RuleFor(c => c.FromDate).ValidSqlDateTime().LessThan(maxHour);
            RuleFor(c => c.ToDate).ValidSqlDateTime();

            RuleFor(c => c.Server).MaximumLength(100);
            RuleFor(c => c.InstanceId).MaximumLength(100);
        }
    }
}