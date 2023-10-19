using FluentValidation;
using Planar.Api.Common.Entities;
using Planar.Service.Model;

namespace Planar.Service.Validation
{
    public class UpdateSummaryReportRequestValidator : AbstractValidator<UpdateSummaryReportRequest>
    {
        public UpdateSummaryReportRequestValidator()
        {
            RuleFor(e => e.Group).Length(2, 50);
            RuleFor(e => e.Group).NotEmpty().When(e => e.Enable).WithMessage("{PropertyName} is mandatory when Enable property is true");
            RuleFor(e => e.Period).IsEnumName(typeof(SummaryReportPeriods), caseSensitive: false);
        }
    }
}