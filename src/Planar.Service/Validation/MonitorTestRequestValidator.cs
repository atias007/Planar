using FluentValidation;
using Planar.API.Common.Entities;
using Planar.Service.Data;
using Planar.Service.General;

namespace Planar.Service.Validation
{
    public class MonitorTestRequestValidator : AbstractValidator<MonitorTestRequest>
    {
        public MonitorTestRequestValidator(GroupData groupData)
        {
            RuleFor(r => r.EffectedRows).GreaterThanOrEqualTo(0);

            RuleFor(r => r.MonitorEvent).IsInEnum()
                .WithMessage("monitor event {PropertyValue} is supported for hook test");

            RuleFor(r => r.DistributionGroupId)
                .Must(id => groupData.IsGroupExists(id).Result)
                .WithMessage("distribution group {PropertyValue} is not exists");

            RuleFor(r => r.Hook)
                .Must(hook => ServiceUtil.MonitorHooks.ContainsKey(hook))
                .WithMessage("hook {PropertyValue} is not exists");
        }
    }
}