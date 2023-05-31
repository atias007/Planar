using System;
using System.ComponentModel.DataAnnotations;

namespace Planar.Validation.Attributes
{
    public class NameAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (Equals(value, null))
            {
                return ValidationResult.Success;
            }

            var stringValue = Convert.ToString(value);

            if (stringValue.Length < 2)
            {
                return new ValidationResult($"{validationContext.MemberName} with value {stringValue} is not valid. minimum length is 2");
            }

            if (stringValue.Length > 50)
            {
                return new ValidationResult($"{validationContext.MemberName} with value {stringValue} is not valid. maximum length is 50");
            }

            return ValidationResult.Success;
        }
    }
}