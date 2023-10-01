using FluentValidation;
using Planar.API.Common.Entities;
using System;

namespace Planar.Service.Validation
{
    public class CounterRequestValidator : AbstractValidator<CounterRequest>
    {
        public CounterRequestValidator()
        {
            RuleFor(r => r.FromDate).ValidSqlDateTime().LessThan(DateTime.Now);
            RuleFor(r => r.ToDate).ValidSqlDateTime();
            RuleFor(r => r.FromDate).LessThan(r => r.ToDate).When(r => r.FromDate.HasValue && r.ToDate.HasValue);
        }
    }
}