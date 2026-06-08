using Microsoft.Extensions.Hosting;
using Planar.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Planar.Job
{
    public class PlanarJobStartProperties
    {
        public static PlanarJobStartProperties Default => new PlanarJobStartProperties();
        public TimeSpan LogFlushTimeout { get; set; } = TimeSpan.FromSeconds(20);
    }

    public class PlanarHostedJobStartProperties<TDefinition> : PlanarJobStartProperties, IHostedJobProperties
        where TDefinition : IJobDefinition
    {
        private readonly List<Type> _hostSingletonTypes = new List<Type>();

        public IEnumerable<Type> HostSingletonTypes => _hostSingletonTypes;

        public string PlanarHostname { get; internal set; } = string.Empty;

        public int PlanarPort { get; internal set; } = 206;

#if NETSTANDARD2_0

        public IHost Host { get; internal set; }
#else
        public IHost Host { get; internal set; } = null!;
#endif

        public IEnumerable<TDefinition> JobDefinitions { get; internal set; } = new List<TDefinition>();
        public IEnumerable<Type> JobTypes => JobDefinitions.Select(jd => jd.JobType);
#if NETSTANDARD2_0

        internal void AddHostSingletonType<T>()
#else
        internal void AddHostSingletonType<T>() where T : notnull
#endif
        {
            var singletonType = typeof(T);
            if (!_hostSingletonTypes.Contains(singletonType))
            {
                _hostSingletonTypes.Add(singletonType);
            }
        }
    }
}