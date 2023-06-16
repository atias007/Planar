using FluentValidation;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.Data;
using Planar.Service.General;

namespace Planar.Service.Validation
{
    public class MonitorTestRequestValidator : AbstractValidator<MonitorTestRequest>
    {
        public MonitorTestRequestValidator(GroupData groupData)
        {
            RuleFor(r => r.EffectedRows).GreaterThanOrEqualTo(0);

            RuleFor(r => r.EventName)
                .NotEmpty()
                .IsInEnum(typeof(MonitorEvents))
                .WithMessage("monitor event {PropertyValue} is supported for hook test");

            RuleFor(r => r.GroupName)
                .NotEmpty()
                .Must(n => groupData.IsGroupNameExists(n).Result)
                .WithMessage("distribution group name '{PropertyValue}' is not exists");

            RuleFor(r => r.Hook)
                .Must(ServiceUtil.MonitorHooks.ContainsKey)
                .WithMessage("hook {PropertyValue} is not exists");
        }
    }
}