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
        public string EncryptionKey { get; set; } = string.Empty;
        internal byte[] EncryptionKeyBytes { get; set; } = Array.Empty<byte>();
    }

    public class PlanarHostedJobStartProperties<TDefinition> : PlanarHostedJobStartProperties, IHostedJobProperties
        where TDefinition : IJobDefinition
    {
        public IEnumerable<TDefinition> JobDefinitions { get; internal set; } = new List<TDefinition>();
        public IEnumerable<Type> JobTypes => JobDefinitions.Select(jd => jd.JobType);
    }

    public class PlanarHostedJobStartProperties : PlanarJobStartProperties
    {
        public const string DefaultPlanarHostname = "localhost";
        public const int DefaultPlanarPort = 206;
        public const int DefaultPlanarApiPort = 2306;

        private readonly List<Type> _hostSingletonTypes = new List<Type>();
        private readonly List<string> _manifests = new List<string>();

        public IEnumerable<Type> HostSingletonTypes => _hostSingletonTypes;

        public string PlanarHostname { get; internal set; } = DefaultPlanarHostname;

        public int PlanarPort { get; internal set; } = DefaultPlanarPort;

        public int PlanarApiPort { get; internal set; } = DefaultPlanarApiPort;

#if NETSTANDARD2_0

        public IHost Host { get; internal set; }
#else
        public IHost Host { get; internal set; } = null!;
#endif

        public IEnumerable<string> Manifests => _manifests;

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

        internal void AddManifests(IEnumerable<string> manifests)
        {
            foreach (var manifest in manifests)
            {
                if (!_manifests.Contains(manifest))
                {
                    _manifests.Add(manifest);
                }
            }
        }
    }
}