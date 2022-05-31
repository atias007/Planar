using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Validation
{
    public class AddJobRequestValidator : AbstractValidator<AddJobRequest>
    {
        public AddJobRequestValidator()
        {
            RuleFor(r => r.Yaml).NotEmpty();
            RuleFor(r => r.Path).NotEmpty();
        }
    }
}