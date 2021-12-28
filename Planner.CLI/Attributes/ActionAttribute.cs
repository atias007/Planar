using System;

namespace Planner.CLI.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class ActionAttribute : Attribute
    {
        public ActionAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }
}