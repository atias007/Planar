using System;
using System.ComponentModel.DataAnnotations;

namespace Planar.Validation.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class LongIdAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (Equals(value, null))
            {
                return ValidationResult.Success;
            }

            var stringValue = Convert.ToString(value);
            if (!long.TryParse(stringValue, out long id))
            {
                return new ValidationResult($"{validationContext.MemberName} is not valid long value");
            }

            if (id <= 0)
            {
                return new ValidationResult($"{validationContext.MemberName} with value {id} is not valid. it should be greater then 0");
            }

            return ValidationResult.Success;
        }
    }
}