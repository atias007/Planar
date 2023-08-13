using System;

namespace Planar.Job
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class IgnoreDataMapAttribute : Attribute
    {
    }
}