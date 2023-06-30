using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation
{
    public class JobOrTriggerKeyValidator : AbstractValidator<JobOrTriggerKey>
    {
        public JobOrTriggerKeyValidator()
        {
            RuleFor(r => r.Id).NotEmpty().MaximumLength(101);
        }
    }
}