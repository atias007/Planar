using FluentValidation;

namespace Planar.Validation
{
    public class UpsertGroupRecordValidator : AbstractValidator<AddGroupRecord>
    {
        public UpsertGroupRecordValidator()
        {
            RuleFor(r => r.Name).NotEmpty().Length(2, 50);
        }
    }
}