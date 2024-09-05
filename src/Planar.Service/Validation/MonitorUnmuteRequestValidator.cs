using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation;

public class MonitorUnmuteRequestValidator : AbstractValidator<MonitorUnmuteRequest>
{
    public MonitorUnmuteRequestValidator()
    {
        RuleFor(x => x.JobId).MaximumLength(101);
        RuleFor(x => x.MonitorId).GreaterThan(0);
    }
}