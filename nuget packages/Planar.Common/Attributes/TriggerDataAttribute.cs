using System;

namespace Planar.Job.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class TriggerDataAttribute : DataAttribute
    {
    }
}