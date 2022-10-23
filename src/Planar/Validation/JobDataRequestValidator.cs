using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Validation
{
    public class JobDataRequestValidator : AbstractValidator<JobDataRequest>
    {
        public JobDataRequestValidator()
        {
            RuleFor(r => r.Id).NotEmpty();
            RuleFor(r => r.DataKey).NotEmpty();
        }
    }
}