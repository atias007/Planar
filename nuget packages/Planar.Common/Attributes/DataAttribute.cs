using System;

namespace Planar.Job.Attributes
{
    public abstract class DataAttribute : Attribute
    {
        public bool ReadOnly { get; set; }
    }
}