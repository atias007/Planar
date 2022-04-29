using System;
using System.Reflection;

namespace Planar.Service.Api.Validation
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public abstract class ValidationBaseAttribute : Attribute
    {
        public abstract void Validate(object value, PropertyInfo propertyInfo);
    }
}