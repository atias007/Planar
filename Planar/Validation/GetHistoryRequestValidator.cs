using FluentValidation;
using Planar.API.Common.Entities;
using System;

namespace Planar.Validation
{
    public class GetHistoryRequestValidator : AbstractValidator<GetHistoryRequest>
    {
        public GetHistoryRequestValidator()
        {
            RuleFor(r => r.Rows).GreaterThan(0);
            RuleFor(r => r.FromDate).LessThan(DateTime.Now);
        }
    }
}