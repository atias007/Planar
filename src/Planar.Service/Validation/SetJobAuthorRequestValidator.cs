using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation
{
    public class SetJobAuthorRequestValidator : AbstractValidator<SetJobAuthorRequest>
    {
        public SetJobAuthorRequestValidator()
        {
            RuleFor(e => e.Author).NotNull().MaximumLength(200);
        }
    }
}