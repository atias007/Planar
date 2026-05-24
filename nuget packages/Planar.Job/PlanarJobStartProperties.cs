using Microsoft.Extensions.Hosting;
using Planar.Common;
using System;
using System.Collections.Generic;
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

#if NETSTANDARD2_0

        public IHost ApplicationHost { get; internal set; }
#else
        public IHost ApplicationHost { get; internal set; } = null!;
#endif

        public IEnumerable<TDefinition> JobDefinitions { get; internal set; } = new List<TDefinition>();
        public IEnumerable<Type> JobTypes => JobDefinitions.Select(jd => jd.JobType);

        internal void AddHostSingletonType<T>()
        {
            var singletonType = typeof(T);
            if (singletonType == null) { throw new ArgumentNullException(nameof(singletonType)); }
            if (!_hostSingletonTypes.Contains(singletonType))
            {
                _hostSingletonTypes.Add(singletonType);
            }
        }
    }
}