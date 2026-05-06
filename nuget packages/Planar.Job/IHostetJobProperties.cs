using System;
using System.Collections.Generic;

namespace Planar.Job
{
    internal interface IHostetJobProperties
    {
        IEnumerable<Type> JobTypes { get; }
    }
}