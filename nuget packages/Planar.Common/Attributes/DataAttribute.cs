using System;

namespace Planar.Job
{
    public abstract class DataAttribute : Attribute
    {
        public bool ReadOnly { get; set; }
    }
}