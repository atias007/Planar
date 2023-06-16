using FluentValidation;
using Planar.API.Common.Entities;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Planar.Service.Validation
{
    public class SetPasswordRequestValidator : AbstractValidator<SetPasswordRequest>
    {
        private const string special = @"#?!@$%^&*:+-()[]|";

        private static readonly string validRegexTemplate = "^([A-Za-z0-9#?!@$%^&*:+\\-\\(\\)\\]\\[|])*$";

        private static readonly char[] specialChars = special.ToCharArray();

        public SetPasswordRequestValidator()
        {
            RuleFor(r => r.Password).NotEmpty().Length(8, 16);
            RuleFor(r => r.Password).Must(r => r.Any(char.IsDigit)).WithMessage("{PropertyName} must contain at least one digit");
            RuleFor(r => r.Password).Must(r => r.Any(char.IsUpper)).WithMessage("{PropertyName} must contain at least one uppercase letter");
            RuleFor(r => r.Password).Must(r => r.Any(char.IsLower)).WithMessage("{PropertyName} must contain at least one lowercase letter");
            RuleFor(r => r.Password).Must(r => !r.Any(char.IsWhiteSpace)).WithMessage("{PropertyName} must not contain space letter");
            RuleFor(r => r.Password).Must(DoesNotContainThreeIdenticalCharsInARow).WithMessage("{PropertyName} must not contain 3 identical letters in a row");
            RuleFor(r => r.Password).Must(r => Regex.IsMatch(r, validRegexTemplate, RegexOptions.None, TimeSpan.FromMilliseconds(500)))
                .WithMessage($"{{PropertyName}} has invalid letters. use alphnumeric and special letters like: {special}");
            RuleFor(r => r.Password).Must(r =>
            {
                foreach (char ch in specialChars)
                {
                    if (r.Contains(ch)) { return true; }
                }

                return false;
            }).WithMessage($"{{PropertyName}} must contains at least 1 special letter ({special})");
        }

        private static bool DoesNotContainThreeIdenticalCharsInARow(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return true;
            }

            int count = 1;
            char prev = str[0];

            for (int i = 1; i < str.Length; i++)
            {
                if (str[i] == prev)
                {
                    count++;

                    if (count == 3)
                    {
                        return false;
                    }
                }
                else
                {
                    count = 1;
                }

                prev = str[i];
            }

            return true;
        }
    }
}