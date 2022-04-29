using Planar.Service.Exceptions;
using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Planar.Service.Api.Validation
{
    public class EmailAttribute : ValidationBaseAttribute
    {
        public override void Validate(object value, PropertyInfo propertyInfo)
        {
            if (value == null) { return; }
            var stringValue = Convert.ToString(value);
            const string pattern = @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9]{2,8}(?:[a-z0-9-]*[a-z0-9])?)\Z";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            if (regex.IsMatch(stringValue) == false)
            {
                var message = $"Property {propertyInfo.Name} has invalid email address value";
                throw new PlanarValidationException(message);
            }
        }
    }
}