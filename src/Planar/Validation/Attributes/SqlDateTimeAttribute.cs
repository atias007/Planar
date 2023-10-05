using Planar.Service.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Planar.Validation.Attributes
{
    public class SqlDateTimeAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (Equals(value, null))
            {
                return ValidationResult.Success;
            }

            var stringValue = Convert.ToString(value);
            if (!DateTime.TryParse(stringValue, CultureInfo.CurrentCulture, out var dateValue))
            {
                return new ValidationResult($"{validationContext.MemberName} is not valid date time value");
            }

            if (!ValidationUtil.IsValidSqlDateTime(dateValue))
            {
                return new ValidationResult($"{validationContext.MemberName} with value {dateValue} is not valid. it should be between 1753-01-01 and 9999-12-31");
            }

            return ValidationResult.Success;
        }
    }
}