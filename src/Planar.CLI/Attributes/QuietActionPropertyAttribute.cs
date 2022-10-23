using System;

namespace Planar.CLI.Attributes
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class QuietActionPropertyAttribute : ActionPropertyAttribute
    {
        public QuietActionPropertyAttribute()
        {
            LongName = "quiet";
            ShortName = "q";
        }
    }
}