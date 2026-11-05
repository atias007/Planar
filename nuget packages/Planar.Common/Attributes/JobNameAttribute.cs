using System;

namespace Planar.Job.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class JobNameAttribute : Attribute
    {
        public JobNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}