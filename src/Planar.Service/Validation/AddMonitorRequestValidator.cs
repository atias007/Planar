using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation;

public class AddMonitorRequestValidator : AbstractValidator<AddMonitorRequest>
{
    public AddMonitorRequestValidator(IValidator<MonitorRequest> monitorValidator)
    {
        Include(monitorValidator);

        RuleFor(r => r.GroupName).NotEmpty().MaximumLength(50);
        RuleFor(r => r.Hook).NotEmpty().MaximumLength(50);
    }
}