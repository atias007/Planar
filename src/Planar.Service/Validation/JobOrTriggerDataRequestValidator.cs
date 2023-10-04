using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation
{
    public class JobOrTriggerDataRequestValidator : AbstractValidator<JobOrTriggerDataRequest>
    {
        public JobOrTriggerDataRequestValidator()
        {
            Include(new JobOrTriggerKeyValidator());

            RuleFor(e => e.DataKey).NotEmpty()
                .MaximumLength(100)
                .Must(Consts.IsDataKeyValid)
                .WithMessage("the data key '{PropertyValue}' in invalid");

            RuleFor(e => e.DataValue)
                .MaximumLength(1000);
        }
    }
}