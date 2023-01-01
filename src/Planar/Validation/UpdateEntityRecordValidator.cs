using FluentValidation;

namespace Planar.Validation
{
    public class UpdateEntityRecordValidator : AbstractValidator<UpdateEntityRecord>
    {
        public UpdateEntityRecordValidator()
        {
            RuleFor(u => u.Id).GreaterThan(0);
            RuleFor(u => u.PropertyName).NotEmpty().NotEqual("id", StringIgnoreCaseComparer.Instance);
            RuleFor(u => u.PropertyValue).NotEmpty();
        }
    }
}