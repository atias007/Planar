using System;

namespace Planar.CLI.Attributes
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