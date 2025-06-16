using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation;

public class UpdateMonitorRequestValidator : AbstractValidator<UpdateMonitorRequest>
{
    public UpdateMonitorRequestValidator(IValidator<MonitorRequest> monitorValidator)
    {
        Include(monitorValidator);
        RuleFor(e => e.Id).GreaterThan(0);
    }
}