using FluentValidation;
using Planar.API.Common.Entities;
using System;

namespace Planar.Service.Validation
{
    public class AllJobsCountersRequestValidator : AbstractValidator<AllJobsCountersRequest>
    {
        public AllJobsCountersRequestValidator()
        {
            RuleFor(e => e.FromDate).ValidSqlDateTime().LessThan(DateTime.Now.Date.AddDays(1));
        }
    }
}