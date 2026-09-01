using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation;

public class ApplyMonitorRequestValidator : AbstractValidator<ApplyMonitorRequest>
{
    public ApplyMonitorRequestValidator(IValidator<MonitorRequest> monitorValidator)
    {
        Include(monitorValidator);

        RuleFor(r => r.Type).Equal("monitor", StringIgnoreCaseComparer.Instance);

        RuleForEach(r => r.DistributionGroups).NotEmpty().MaximumLength(50);

        RuleFor(r => r.DistributionGroups).NotEmpty().WithMessage("at least one distribution group is required.");

        RuleForEach(r => r.Hooks).NotEmpty().MaximumLength(50);

        RuleFor(r => r.Hooks).NotEmpty();
    }
}