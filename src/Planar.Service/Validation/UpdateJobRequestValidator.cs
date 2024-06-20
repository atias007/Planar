using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation
{
    public class UpdateJobRequestValidator : AbstractValidator<UpdateJobRequest>
    {
        public UpdateJobRequestValidator()
        {
            RuleFor(r => r.JobFilePath).NotEmpty().Length(2, 500).Path();
        }
    }
}