using FluentValidation;
using Planar.API.Common.Entities;
using Planar.Service.Validation;

namespace Planar.Validation
{
    public class AddJobFoldeRequestValidator : AbstractValidator<SetJobPathRequest>
    {
        public AddJobFoldeRequestValidator()
        {
            RuleFor(r => r.Folder).NotEmpty().Length(2, 500).Path();
            RuleFor(r => r.JobFileName).NotEmpty().Length(2, 500);
        }
    }
}