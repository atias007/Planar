using FluentValidation;

namespace Planar.Service.Validation
{
    public class SqlStepValidator : AbstractValidator<SqlStep>
    {
        public SqlStepValidator()
        {
            RuleFor(s => s.Name).NotEmpty().Length(1, 50);
            RuleFor(s => s.Filename).NotEmpty().Length(1, 100);
        }
    }
}