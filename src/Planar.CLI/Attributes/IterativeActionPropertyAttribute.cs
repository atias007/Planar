using System;

namespace Planar.CLI.Attributes
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class IterativeActionPropertyAttribute : ActionPropertyAttribute
    {
        public const string LongNameText = "iterative";
        public const string ShortNameText = "it";

        public IterativeActionPropertyAttribute()
        {
            LongName = LongNameText;
            ShortName = ShortNameText;
        }
    }
}