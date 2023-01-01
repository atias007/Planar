using FluentValidation;
using Planar.API.Common.Entities;
using Planar.Service.API.Helpers;
using Planar.Service.Data;

namespace Planar.Service.Validation
{
    public class AddMonitorRequestValidator : AbstractValidator<AddMonitorRequest>
    {
        public AddMonitorRequestValidator(GroupData dal, JobKeyHelper jobKeyHelper)
        {
            RuleFor(r => r.Title).NotEmpty().Length(5, 50);
            RuleFor(r => r.EventArgument).MaximumLength(50);
            RuleFor(r => r).JobAndGroupExists(jobKeyHelper);
            RuleFor(r => r.JobGroup).NotEmpty().When(r => !string.IsNullOrEmpty(r.JobName)).WithMessage("{PropertyName} must have value if 'Job Name' is not empty"); ;
            RuleFor(r => r.EventId).IsInEnum();
            RuleFor(r => r.GroupId).GreaterThan(0).Must(g => dal.IsGroupExists(g).Result).WithMessage("'{PropertyName}' field with value '{PropertyValue}' does not exist");
            RuleFor(r => r.Hook).NotEmpty().Must(ValidationUtil.IsHookExists).WithMessage("'{PropertyName}' field with value '{PropertyValue}' does not exist");
        }
    }
}