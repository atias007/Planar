using System;

namespace Planar.CLI.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class ModuleAttribute : Attribute
    {
        public ModuleAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }
}