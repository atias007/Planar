using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;

namespace Planar.Common
{
    public interface IHostedJobProperties
    {
        string PlanarHostname { get; }
        IEnumerable<Type> JobTypes { get; }
        IEnumerable<Type> HostSingletonTypes { get; }
        IHost ApplicationHost { get; }
    }

    public interface IJobDefinition
    {
        Type JobType { get; }
    }
}