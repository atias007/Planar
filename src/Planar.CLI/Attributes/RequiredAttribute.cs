using System;

namespace Planar.CLI.Attributes
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    internal class RequiredAttribute : Attribute
    {
        public RequiredAttribute()
        {
        }

        public RequiredAttribute(string message)
        {
            Message = message;
        }

        public string? Message { get; private set; }
    }
}