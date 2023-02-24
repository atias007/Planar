using System;

namespace Planar.CLI.Attributes
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class ActionEnumOptionAttribute : Attribute
    {
        public ActionEnumOptionAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}