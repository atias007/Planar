using FluentValidation;

namespace Planar.Service.Validation
{
    public static class CustomValidators
    {
        public static IRuleBuilderOptions<T, string> OnlyDigits<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.Must(v => ValidationUtil.IsOnlyDigits(v)).WithMessage("'{PropertyName}' must have only digits");
        }

        public static IRuleBuilderOptions<T, string> JobIdExists<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.Must(v => ValidationUtil.IsJobExists(v)).WithMessage("'{PropertyName}' field with value '{PropertyValue}' does not exist");
        }
    }
}