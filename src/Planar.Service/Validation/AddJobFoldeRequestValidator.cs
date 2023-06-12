using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation
{
    public class AddJobFoldeRequestValidator : AbstractValidator<SetJobPathRequest>
    {
        public AddJobFoldeRequestValidator()
        {
            RuleFor(r => r.Folder).NotEmpty().Length(2, 500).Path();
            RuleFor(r => r.JobFileName).MaximumLength(500);
        }
    }
}