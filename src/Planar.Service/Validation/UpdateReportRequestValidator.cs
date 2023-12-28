using FluentValidation;
using Planar.API.Common.Entities;
using Planar.Service.Reports;

namespace Planar.Service.Validation
{
    public class UpdateReportRequestValidator : AbstractValidator<UpdateReportRequest>
    {
        public UpdateReportRequestValidator()
        {
            RuleFor(e => e.Group).Length(2, 50);
            RuleFor(e => e.HourOfDay).InclusiveBetween(0, 23);
            RuleFor(e => e.Period).NotEmpty().IsEnumName(typeof(ReportPeriods), caseSensitive: false);
        }
    }
}