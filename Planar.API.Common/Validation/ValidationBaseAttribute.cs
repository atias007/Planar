using System;

namespace Planar.API.Common.Validation
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public abstract class ValidationBaseAttribute : Attribute
    {
    }

    public class EmailAttribute : ValidationBaseAttribute
    {
    }

    public class RequiredAttribute : ValidationBaseAttribute
    {
    }

    public class LengthAttribute : ValidationBaseAttribute
    {
        public LengthAttribute(int minimum, int maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        public int? Minimum { get; set; }
        public int? Maximum { get; set; }
    }

    public class RangeAttribute : ValidationBaseAttribute
    {
        public RangeAttribute(int? minimum, int? maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        public int? Minimum { get; set; }
        public int? Maximum { get; set; }
    }

    public class TrimAttribute : ValidationBaseAttribute
    {
    }

    public class NumericAttribute : ValidationBaseAttribute
    {
    }
}