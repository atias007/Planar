using System;
using System.Collections.Generic;

namespace Planar.Common
{
    public interface IHostetJobProperties
    {
        string PlanarHostname { get; }
        IEnumerable<Type> JobTypes { get; }
    }
}