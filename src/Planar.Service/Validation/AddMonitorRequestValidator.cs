using FluentValidation;
using Planar.API.Common.Entities;
using Planar.Service.Data;

namespace Planar.Service.Validation;

public class AddMonitorRequestValidator : AbstractValidator<AddMonitorRequest>
{
    public AddMonitorRequestValidator(IValidator<MonitorRequest> monitorValidator, IGroupData dal)
    {
        Include(monitorValidator);

        RuleFor(r => r.GroupName).NotEmpty();

        RuleFor(r => r.GroupName)
            .Must(g => dal.IsGroupNameExists(g).Result)
            .When(r => !string.IsNullOrWhiteSpace(r.GroupName))
            .WithMessage("{PropertyName} '{PropertyValue}' does not exist");
    }
}