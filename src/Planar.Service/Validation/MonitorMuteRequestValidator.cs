using FluentValidation;
using Planar.API.Common.Entities;
using Planar.Common;
using System;

namespace Planar.Service.Validation
{
    public class MonitorMuteRequestValidator : AbstractValidator<MonitorMuteRequest>
    {
        public MonitorMuteRequestValidator()
        {
            Include(new MonitorUnmuteRequestValidator());
            RuleFor(x => x.DueDate)
                .NotEmpty()
                .GreaterThan(DateTime.Now)
                .LessThanOrEqualTo(DateTime.Now.Add(AppSettings.Monitor.ManualMuteMaxPeriod));
        }
    }
}