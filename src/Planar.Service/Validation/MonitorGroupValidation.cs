using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation;

internal class MonitorGroupValidation : AbstractValidator<MonitorGroupRequest>
{
    public MonitorGroupValidation()
    {
        RuleFor(g => g.GroupName).NotEmpty().Length(2, 50);
        RuleFor(g => g.MonitorId).GreaterThan(0);
    }
}