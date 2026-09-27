using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.Data;
using Planar.Service.General;

namespace Planar.Service.Validation;

public class MonitorTestRequestValidator : AbstractValidator<MonitorTestRequest>
{
    public MonitorTestRequestValidator(IServiceScopeFactory serviceScopeFactory)
    {
        RuleFor(r => r.EffectedRows).GreaterThanOrEqualTo(0);

        RuleFor(r => r.EventName)
            .NotEmpty()
            .IsInEnum(typeof(MonitorEvents))
            .WithMessage("monitor event {PropertyValue} is supported for hook test");

        RuleFor(r => r.GroupName)
            .NotEmpty()
            .MustAsync(async (n, _) =>
            {
                await using var scope = serviceScopeFactory.CreateAsyncScope();
                var groupData = scope.ServiceProvider.GetRequiredService<IGroupData>();
                return await groupData.IsGroupNameExists(n);
            })
            .WithMessage("distribution group name '{PropertyValue}' is not exists");

        RuleFor(r => r.Hook)
            .Must(ServiceUtil.MonitorHooks.ContainsKey)
            .WithMessage("hook {PropertyValue} is not exists");
    }
}