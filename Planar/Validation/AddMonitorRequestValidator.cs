using FluentValidation;
using Planar.API.Common.Entities;
using Planar.Service.Data;
using Planar.Service.Validation;

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
            RuleFor(r => r.MonitorEvent).GreaterThan(0).Must(ValidationUtil.IsEventExists).WithMessage("'{PropertyName}' field with value {PropertyValue} does not exist");
            RuleFor(r => r.GroupId).GreaterThan(0).Must(g => ValidationUtil.IsGroopExists(g, dal)).WithMessage("'{PropertyName}' field with value {PropertyValue} does not exist");
            RuleFor(r => r.Hook).NotEmpty().Must(ValidationUtil.IsHookExists).WithMessage("'{PropertyName}' field with value {PropertyValue} does not exist");
        }
    }
}