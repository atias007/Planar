using Planar.Service.Exceptions;
using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Planar.Service.Api.Validation
{
    public class NumericAttribute : ValidationBaseAttribute
    {
        public override void Validate(object value, PropertyInfo propertyInfo)
        {
            if (value == null) { return; }
            var stringValue = Convert.ToString(value);
            const string pattern = "^[0-9]+$";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            if (regex.IsMatch(stringValue) == false)
            {
                var message = $"Property {propertyInfo.Name} has invalid numeric value";
                throw new PlanarValidationException(message);
            }
        }
    }
}