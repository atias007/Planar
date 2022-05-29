using Planar.Service.Exceptions;
using System;
using System.Reflection;

namespace Planar.Service.Api.Validation
{
    public class RequiredAttribute : ValidationBaseAttribute
    {
        public override void Validate(object value, PropertyInfo propertyInfo)
        {
            var stringValue = Convert.ToString(value);
            var type = propertyInfo.PropertyType;
            var isEmpty = false;

            if (string.IsNullOrEmpty(stringValue)) { isEmpty = true; }
            else if (type == typeof(DateTime))
            {
                if (Convert.ToDateTime(value) == DateTime.MinValue) { isEmpty = true; }
            }
            else if (type == typeof(Guid))
            {
                if ((Guid)value == Guid.Empty) { isEmpty = true; }
            }
            else if (type.Namespace == "System.Collections" || type.Namespace == "System.Collections.Generic")
            {
                var method = value.GetType().GetMethod("get_Count");
                var count = method == null ? -1 : Convert.ToInt32(method.Invoke(value, null));
                isEmpty = count == 0;
            }

            if (isEmpty)
            {
                var message = $"Property {propertyInfo.Name} is required but does not contain a value";
                throw new RestValidationException(message);
            }
        }
    }
}