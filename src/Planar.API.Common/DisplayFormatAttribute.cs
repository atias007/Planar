using System;

namespace Planar.API.Common
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DisplayFormatAttribute : Attribute
    {
        public DisplayFormatAttribute(string format)
        {
            Format = format;
        }

        public string Format { get; }
    }
}