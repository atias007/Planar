using FluentValidation;
using Planar.API.Common.Entities;
using Planar.Service.Reports;
using System;

namespace Planar.Service.Validation
{
    public class RunReportRequestValidator : AbstractValidator<RunReportRequest>
    {
        public RunReportRequestValidator()
        {
            RuleFor(e => e.Group).Length(2, 50);
            RuleFor(e => e.Period).IsEnumName(typeof(ReportPeriods), caseSensitive: false);

            RuleFor(r => r.FromDate).ValidSqlDateTime().LessThan(DateTime.Now);
            RuleFor(r => r.ToDate).ValidSqlDateTime();
            RuleFor(r => r.FromDate).LessThan(r => r.ToDate).When(r => r.FromDate.HasValue && r.ToDate.HasValue);
        }
    }
}