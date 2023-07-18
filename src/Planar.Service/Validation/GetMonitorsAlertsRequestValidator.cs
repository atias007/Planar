using FluentValidation;
using Planar.API.Common.Entities;
using Planar.Common;
using System;

namespace Planar.Service.Validation
{
    public class GetMonitorsAlertsRequestValidator : AbstractValidator<GetMonitorsAlertsRequest>
    {
        public GetMonitorsAlertsRequestValidator()
        {
            Include(new PagingRequestValidator());

            RuleFor(r => r.FromDate).ValidSqlDateTime().LessThan(DateTime.Now);
            RuleFor(r => r.ToDate).ValidSqlDateTime().LessThan(DateTime.Now);
            RuleFor(r => r.FromDate).LessThan(r => r.ToDate).When(r => r.FromDate.HasValue && r.ToDate.HasValue);
            RuleFor(e => e.EventTitle).IsInEnum(typeof(MonitorEvents))
                .WithMessage("'{PropertyName}' has a range of values which does not include '{PropertyValue}'. use /monitor/events to get all allowed values");

            RuleFor(e => e.JobId).MaximumLength(20);
            RuleFor(r => r.JobGroup).MaximumLength(50);
            RuleFor(r => r.GroupName).MaximumLength(50);
            RuleFor(r => r.Hook).MaximumLength(50);

            RuleFor(r => r.JobId).Null()
                .When((req, r) => !string.IsNullOrEmpty(req.JobGroup))
                .WithMessage("{PropertyName} must be null when 'Group' property is provided");

            RuleFor(r => r.JobGroup).Null()
                .When((req, r) => !string.IsNullOrEmpty(req.JobId))
                .WithMessage("{PropertyName} must be null when 'JobId' property is provided");
        }
    }
}