using FluentValidation;
using Planar.API.Common.Entities;
using System;

namespace Planar.Service.Validation
{
    public class GetTraceRequestValidator : AbstractValidator<GetTraceRequest>
    {
        public GetTraceRequestValidator()
        {
            Include(new PagingRequestValidator());
            RuleFor(e => e.Level).MaximumLength(50);
            RuleFor(e => e.FromDate).ValidSqlDateTime().LessThan(DateTime.Now);
            RuleFor(e => e.ToDate).ValidSqlDateTime().LessThan(DateTime.Now);
            RuleFor(e => e.FromDate).LessThan(e => e.ToDate).When(e => e.FromDate != null && e.ToDate != null);
        }
    }
}
