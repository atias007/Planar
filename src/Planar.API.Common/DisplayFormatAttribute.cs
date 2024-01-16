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

        public DisplayFormatAttribute(string format, IFormatProvider formatProvider) : this(format)
        {
            FormatProvider = formatProvider;
        }

        public string Format { get; }

        public IFormatProvider? FormatProvider { get; }
    }
}