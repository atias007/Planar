using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation
{
    public class UpdateIntervalRequestValidator : AbstractValidator<UpdateIntervalRequest>
    {
        public UpdateIntervalRequestValidator()
        {
            Include(new JobOrTriggerKeyValidator());
            RuleFor(x => x.Interval).NotEmpty();
        }
    }
}