using FluentValidation;

namespace Planar.Validation
{
    public class UpsertGroupRecordValidator : AbstractValidator<UpsertGroupRecord>
    {
        public UpsertGroupRecordValidator()
        {
            RuleFor(r => r.Id).GreaterThan(0);
            RuleFor(r => r.Name).NotEmpty().Length(2, 50);
        }
    }
}