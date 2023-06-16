﻿using FluentValidation.Validators;
using System;
using System.Globalization;
using System.Reflection;

namespace FluentValidation;

public class EnumValidatorForInteger<T, TProperty> : PropertyValidator<T, TProperty>
{
    private readonly Type _enumType;

    public EnumValidatorForInteger(Type enumType)
    {
        _enumType = enumType;
    }

    public override string Name => "EnumValidator";

    public override bool IsValid(ValidationContext<T> context, TProperty value)
    {
        if (value == null) { return true; }

        var underlyingEnumType = Nullable.GetUnderlyingType(_enumType) ?? _enumType;

        if (!underlyingEnumType.IsEnum) { return false; }

        if (underlyingEnumType.GetCustomAttribute<FlagsAttribute>() != null)
        {
            return IsFlagsEnumDefined(underlyingEnumType, value);
        }

        return Enum.IsDefined(underlyingEnumType, value);
    }

    private static bool IsFlagsEnumDefined(Type enumType, object value)
    {
        var typeName = Enum.GetUnderlyingType(enumType).Name;

        switch (typeName)
        {
            case "Byte":
                {
                    var typedValue = (byte)value;
                    return EvaluateFlagEnumValues(typedValue, enumType);
                }

            case "Int16":
                {
                    var typedValue = (short)value;

                    return EvaluateFlagEnumValues(typedValue, enumType);
                }

            case "Int32":
                {
                    var typedValue = (int)value;

                    return EvaluateFlagEnumValues(typedValue, enumType);
                }

            case "Int64":
                {
                    var typedValue = (long)value;

                    return EvaluateFlagEnumValues(typedValue, enumType);
                }

            case "SByte":
                {
                    var typedValue = (sbyte)value;

                    return EvaluateFlagEnumValues(Convert.ToInt64(typedValue), enumType);
                }

            case "UInt16":
                {
                    var typedValue = (ushort)value;
                    return EvaluateFlagEnumValues(typedValue, enumType);
                }

            case "UInt32":
                {
                    var typedValue = (uint)value;
                    return EvaluateFlagEnumValues(typedValue, enumType);
                }

            case "UInt64":
                {
                    var typedValue = (ulong)value;
                    return EvaluateFlagEnumValues((long)typedValue, enumType);
                }

            default:
                var message = $"Unexpected typeName of '{typeName}' during flags enum evaluation.";
                throw new ArgumentOutOfRangeException(nameof(enumType), message);
        }
    }

    private static bool EvaluateFlagEnumValues(long value, Type enumType)
    {
        long mask = 0;
        foreach (var enumValue in Enum.GetValues(enumType))
        {
            var enumValueAsInt64 = Convert.ToInt64(enumValue, CultureInfo.CurrentCulture);
            if ((enumValueAsInt64 & value) == enumValueAsInt64)
            {
                mask |= enumValueAsInt64;
                if (mask == value) { return true; }
            }
        }

        return false;
    }

    protected override string GetDefaultMessageTemplate(string errorCode)
    {
        return Localized(errorCode, Name);
    }
}