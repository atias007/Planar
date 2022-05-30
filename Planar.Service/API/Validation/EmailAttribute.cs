using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Planar.Service.Api.Validation
{
    public class EmailAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value == null) { return false; }
            var stringValue = Convert.ToString(value);
            const string pattern = @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9]{2,8}(?:[a-z0-9-]*[a-z0-9])?)\Z";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            return regex.IsMatch(stringValue);
        }

        public override string FormatErrorMessage(string name)
        {
            return $"{name} is not valid email address";
        }
    }
}