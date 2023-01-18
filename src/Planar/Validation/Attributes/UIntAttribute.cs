using System;
using System.ComponentModel.DataAnnotations;

namespace Planar.Validation.Attributes
{
    public class UIntAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (Equals(value, null))
            {
                return ValidationResult.Success;
            }

            var stringValue = Convert.ToString(value);
            if (!int.TryParse(stringValue, out int id))
            {
                return new ValidationResult($"{validationContext.MemberName} is not valid integer value");
            }

            if (id < 0)
            {
                return new ValidationResult($"{validationContext.MemberName} with value {id} is not valid. It should be greater then or equals 0");
            }

            return ValidationResult.Success;
        }
    }
}