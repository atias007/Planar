using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Planar.Service.Api.Validation
{
    public class NumericAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value == null) { return false; }
            var stringValue = Convert.ToString(value);
            const string pattern = "^[0-9]+$";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            return regex.IsMatch(stringValue);
        }

        public override string FormatErrorMessage(string name)
        {
            return $"{name} is not valid numeric value";
        }
    }
}