using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation
{
    public class UpdateJobRequestValidator : AbstractValidator<UpdateJobRequest>
    {
        public UpdateJobRequestValidator()
        {
            Include(new JobOrTriggerKeyValidator());
        }
    }
}