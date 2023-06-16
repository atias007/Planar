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
}