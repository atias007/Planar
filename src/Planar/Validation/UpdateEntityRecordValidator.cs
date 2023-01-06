using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Validation
{
    public class UpdateEntityRecordValidator : AbstractValidator<UpdateEntityRequest>
    {
        public UpdateEntityRecordValidator()
        {
            RuleFor(u => u.Id).GreaterThan(0);
            RuleFor(u => u.PropertyName).NotEmpty().NotEqual("id", StringIgnoreCaseComparer.Instance);
            RuleFor(u => u.PropertyValue).NotEmpty();
        }
    }
}