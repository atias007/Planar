using Planner.Service.Exceptions;
using System;
using System.Reflection;

namespace Planner.Service.Api.Validation
{
    public class LengthAttribute : ValidationBaseAttribute
    {
        public LengthAttribute()
        {
        }

        public LengthAttribute(int? minimum, int? maximum)
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

            if (Minimum.HasValue && stringValue.Length < Minimum)
            {
                var message = $"Property {propertyInfo.Name} length {stringValue.Length} is less then minumum length of {Minimum}";
                throw new PlannerValidationException(message);
            }

            if (Maximum.HasValue && stringValue.Length > Maximum)
            {
                var message = $"Property {propertyInfo.Name} length {stringValue.Length} is more then maximum length of {Maximum}";
                throw new PlannerValidationException(message);
            }
        }
    }
}