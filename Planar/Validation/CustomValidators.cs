using FluentValidation;
using System.Text.RegularExpressions;

namespace Planar.Validation
{
    public static class CustomValidators
    {
        public static IRuleBuilderOptions<T, string> OnlyDigits<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.Must(v => IsOnlyDigits(v)).WithMessage("'{PropertyName}' must have only digits");
        }

        public static bool IsOnlyDigits(string value)
        {
            if (value == null) { return false; }
            const string pattern = "^[0-9]+$";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            return regex.IsMatch(value);
        }
    }
}