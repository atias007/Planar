using Planar.API.Common.Entities;
using Planar.Service.API.Helpers;
using Planar.Service.Validation;
using System;

namespace FluentValidation
{
    public static class CustomValidators
    {
        public static IRuleBuilderOptions<T, string> OnlyDigits<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .Must(ValidationUtil.IsOnlyDigits)
                .WithMessage("{PropertyName} must have only digits");
        }

        public static IRuleBuilderOptions<T, string> JobIdExists<T>(this IRuleBuilder<T, string> ruleBuilder, JobKeyHelper jobKeyHelper)
        {
            return ruleBuilder
                .Must(v => ValidationUtil.IsJobIdExists(v, jobKeyHelper))
                .WithMessage("{PropertyName} field with value '{PropertyValue}' does not exist");
        }

        public static IRuleBuilderOptions<T, string> Path<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .Must(ValidationUtil.IsPath)
                .WithMessage("{PropertyName} field with value '{PropertyValue}' is not valid path");
        }

        /// <summary>
        /// Validate that datetime value is valid for sql server
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ruleBuilder"></param>
        /// <returns></returns>
        public static IRuleBuilderOptions<T, DateTime> ValidSqlDateTime<T>(this IRuleBuilder<T, DateTime> ruleBuilder)
        {
            return ruleBuilder.Must(ValidationUtil.IsValidSqlDateTime)
                .WithMessage("'{PropertyName}' with value '{PropertyValue}' is not valid database datetime value");
        }

        /// <summary>
        /// Validate that datetime value is valid for sql server
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ruleBuilder"></param>
        /// <returns></returns>
        public static IRuleBuilderOptions<T, DateTime?> ValidSqlDateTime<T>(this IRuleBuilder<T, DateTime?> ruleBuilder)
        {
            return ruleBuilder
                .Must(ValidationUtil.IsValidSqlDateTime)
                .WithMessage("'{PropertyName}' with value '{PropertyValue}' is not valid database datetime value");
        }
    }
}