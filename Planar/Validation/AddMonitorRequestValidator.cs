using FluentValidation;
using Planar.API.Common.Entities;
using Planar.Service.Data;
using Planar.Service.General;
using System;

namespace Planar.Validation
{
    public class AddMonitorRequestValidator : AbstractValidator<AddMonitorRequest>
    {
        public AddMonitorRequestValidator(DataLayer dal)
        {
            RuleFor(r => r.Title).NotEmpty().Length(5, 50);
            RuleFor(r => r.EventArguments).MaximumLength(50);
            RuleFor(r => r.JobId).JobIdExists().NotEmpty().When(r => string.IsNullOrEmpty(r.JobGroup)).WithMessage("'{PropertyName}' must have value if 'Job Group' is empty");
            RuleFor(r => r.JobGroup).NotEmpty().When(r => string.IsNullOrEmpty(r.JobId)).WithMessage("'{PropertyName}' must have value if 'Job Id' is empty"); ;
            RuleFor(r => r.MonitorEvent).GreaterThan(0).Must(IsEventExists).WithMessage("'{PropertyName}' field with value {PropertyValue} does not exist");
            RuleFor(r => r.GroupId).GreaterThan(0).Must(g => IsGroopExists(g, dal)).WithMessage("'{PropertyName}' field with value {PropertyValue} does not exist");
            RuleFor(r => r.Hook).NotEmpty().Must(IsHookExists).WithMessage("'{PropertyName}' field with value {PropertyValue} does not exist");
        }

        private static bool IsEventExists(int monitorEvent)
        {
            var exists = Enum.IsDefined(typeof(MonitorEvents), monitorEvent);
            return exists;
        }

        private static bool IsHookExists(string hook)
        {
            var exists = ServiceUtil.MonitorHooks.ContainsKey(hook);
            return exists;
        }

        private static bool IsGroopExists(int groop, DataLayer dal)
        {
            var exists = dal.IsGroupExists(groop).Result;
            return exists;
        }
    }
}