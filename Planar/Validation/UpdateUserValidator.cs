using FluentValidation;

namespace Planar.Validation
{
    public class UpdateUserValidator : AddUserValidator
    {
        public UpdateUserValidator() : base()
        {
            RuleFor(u => u.Id).GreaterThan(0);
        }
    }
}