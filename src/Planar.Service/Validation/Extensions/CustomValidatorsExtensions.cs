using System;

namespace FluentValidation;

public static class CustomValidatorsExtensions
{
    /// <summary>
    /// Validate that string property is one of values in specified enum (case insesitive)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="ruleBuilder"></param>
    /// <param name="enumType"></param>
    /// <returns></returns>
    public static IRuleBuilderOptions<T, TProperty> IsInEnum<T, TProperty>(this IRuleBuilder<T, TProperty> ruleBuilder, Type enumType)
    {
        var validator = new EnumValidatorForInteger<T, TProperty>(enumType);
        return ruleBuilder.SetValidator(validator);
    }

    public static IRuleBuilderOptions<T, string> CronExpression<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Must(Quartz.CronExpression.IsValidExpression).WithMessage("invalid cron expression");
    }

    public static IRuleBuilderOptions<T, TimeSpan?> NotZero<T>(this IRuleBuilder<T, TimeSpan?> ruleBuilder)
    {
        return ruleBuilder.Must(b => b == null || b != TimeSpan.Zero).WithMessage("time span must be different from zero");
    }

    public static IRuleBuilderOptions<T, string?> IsUri<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder.Must(b => b == null || IsValidUri(b)).WithMessage("{PropertyName} has invalid url format");
    }

    private static bool IsValidUri(string uriString)
    {
        return Uri.TryCreate(uriString, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeFile || uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}