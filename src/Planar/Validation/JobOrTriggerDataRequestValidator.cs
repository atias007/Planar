using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Validation
{
    public class JobOrTriggerDataRequestValidator : AbstractValidator<JobOrTriggerDataRequest>
    {
        public JobOrTriggerDataRequestValidator()
        {
            RuleFor(e => e.DataKey).NotEmpty()
                .MaximumLength(100)
                .Must(key => Consts.IsDataKeyValid(key))
                .WithMessage("the data key '{PropertyValue}' in invalid");

            RuleFor(e => e.DataValue)
                .MaximumLength(1000)
                .NotEmpty();
        }
    }
}