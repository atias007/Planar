using System;

namespace Planner.CLI.Attributes
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class IterativeActionPropertyAttribute : ActionPropertyAttribute
    {
        public IterativeActionPropertyAttribute()
        {
            LongName = "iterative";
            ShortName = "it";
        }
    }
}