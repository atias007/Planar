using FluentValidation;
using Planar.Service.API.Helpers;
using Planar.Service.Data;
using Planar.Service.Model;

namespace Planar.Service.Validation
{
    public class MonitorActionValidator : AbstractValidator<MonitorAction>
    {
        public MonitorActionValidator(GroupData dal, JobKeyHelper jobKeyHelper)
        {
            RuleFor(r => r.Title).NotEmpty().Length(5, 50);
            RuleFor(r => r.EventArgument).MaximumLength(50);
            RuleFor(r => r.JobId).JobIdExists(jobKeyHelper).NotEmpty().When(r => string.IsNullOrEmpty(r.JobGroup)).WithMessage("'{PropertyName}' must have value if 'Job Group' is empty");
            RuleFor(r => r.JobGroup).NotEmpty().When(r => string.IsNullOrEmpty(r.JobId)).WithMessage("'{PropertyName}' must have value if 'Job Id' is empty"); ;
            RuleFor(r => r.EventId).GreaterThan(0).Must(ValidationUtil.IsEventExists).WithMessage("'{PropertyName}' field with value {PropertyValue} does not exist");
            RuleFor(r => r.GroupId).GreaterThan(0).Must(g => dal.IsGroupExists(g).Result).WithMessage("'{PropertyName}' field with value {PropertyValue} does not exist");
            RuleFor(r => r.Hook).NotEmpty().Must(ValidationUtil.IsHookExists).WithMessage("'{PropertyName}' field with value {PropertyValue} does not exist");
        }
    }
}