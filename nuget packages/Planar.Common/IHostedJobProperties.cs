using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;

namespace Planar.Common
{
    public interface IHostedJobProperties
    {
        string PlanarHostname { get; }
        int PlanarPort { get; }
        IEnumerable<Type> JobTypes { get; }
        IEnumerable<Type> HostSingletonTypes { get; }
        IHost Host { get; }
    }

    public interface IJobDefinition
    {
        Type JobType { get; }
    }
}