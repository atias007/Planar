using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation
{
    public class UpdateCronRequestValidator : AbstractValidator<UpdateCronRequest>
    {
        public UpdateCronRequestValidator()
        {
            Include(new JobOrTriggerKeyValidator());
            RuleFor(x => x.CronExpression).NotEmpty().CronExpression();
        }
    }
}