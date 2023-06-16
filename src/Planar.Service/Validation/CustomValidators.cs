using FluentValidation;
using Planar.Service.API.Helpers;

namespace Planar.Service.Validation
{
    public static class CustomValidators
    {
        public static IRuleBuilderOptions<T, string> OnlyDigits<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.Must(v => ValidationUtil.IsOnlyDigits(v)).WithMessage("{PropertyName} must have only digits");
        }

        public static IRuleBuilderOptions<T, string> JobIdExists<T>(this IRuleBuilder<T, string> ruleBuilder, JobKeyHelper jobKeyHelper)
        {
            return ruleBuilder.Must(v => ValidationUtil.IsJobIdExists(v, jobKeyHelper)).WithMessage("{PropertyName} field with value '{PropertyValue}' does not exist");
        }

        public static IRuleBuilderOptions<T, string> Path<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.Must(ValidationUtil.IsPath).WithMessage("{PropertyName} field with value '{PropertyValue}' is not valid path");
        }
    }
}