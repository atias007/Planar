using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation
{
    public class UpdateTimeoutRequestValidator : AbstractValidator<UpdateTimeoutRequest>
    {
        public UpdateTimeoutRequestValidator()
        {
            Include(new JobOrTriggerKeyValidator());
            RuleFor(x => x.Timeout).NotZero();
        }
    }
}