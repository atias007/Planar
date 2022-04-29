using Planar.Service.Exceptions;
using System;
using System.Reflection;

namespace Planar.Service.Api.Validation
{
    public class RangeAttribute : ValidationBaseAttribute
    {
        public RangeAttribute()
        {
        }

        public RangeAttribute(int? minimum, int? maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        public int? Minimum { get; set; }

        public int? Maximum { get; set; }

        public override void Validate(object value, PropertyInfo propertyInfo)
        {
            if (value == null) { return; }
            var stringValue = Convert.ToString(value);

            var valid = double.TryParse(stringValue, out var numericValue);
            if (valid == false)
            {
                var message = $"Property {propertyInfo.Name} has invalid value for Range validation attribute";
                throw new PlanarValidationException(message);
            }

            if (Minimum.HasValue && numericValue < Minimum)
            {
                var message = $"Property {propertyInfo.Name} value {numericValue} is less then minumum value of {Minimum}";
                throw new PlanarValidationException(message);
            }

            if (Maximum.HasValue && numericValue > Maximum)
            {
                var message = $"Property {propertyInfo.Name} value {numericValue} is more then maximum value of {Maximum}";
                throw new PlanarValidationException(message);
            }
        }
    }
}