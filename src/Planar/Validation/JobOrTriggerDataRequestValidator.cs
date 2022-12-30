using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Validation
{
    public class JobOrTriggerDataRequestValidator : AbstractValidator<JobOrTriggerDataRequest>
    {
        public JobOrTriggerDataRequestValidator()
        {
            RuleFor(e => e.DataKey).NotEmpty();
            RuleFor(e => e.DataValue).NotEmpty();
        }
    }
}