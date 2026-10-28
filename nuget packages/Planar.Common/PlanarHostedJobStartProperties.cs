using Microsoft.Extensions.Hosting;
using Planar.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Planar.Job
{
    public class PlanarHostedJobStartProperties<TDefinition> : PlanarHostedJobStartProperties, IHostedJobProperties
    where TDefinition : IJobDefinition
    {
        private readonly Dictionary<Type, TDefinition> _jobDefinitions = new Dictionary<Type, TDefinition>();

        public IEnumerable<TDefinition> JobDefinitions => _jobDefinitions.Values;
        public IEnumerable<Type> JobTypes => JobDefinitions.Select(jd => jd.JobType);

        internal void AddJobDefinition(TDefinition jobDefinition)
        {
            if (_jobDefinitions.ContainsKey(jobDefinition.JobType))
            {
                throw new InvalidOperationException($"Job definition for job type '{jobDefinition.JobType.FullName}' already exists.");
            }

            _jobDefinitions.Add(jobDefinition.JobType, jobDefinition);
        }
    }

    public class PlanarHostedJobStartProperties : PlanarJobStartProperties
    {
        public const string DefaultPlanarHostname = "localhost";
        public const int DefaultPlanarPort = 206;
        public const int DefaultPlanarApiPort = 2306;

        private readonly List<Type> _hostSingletonTypes = new List<Type>();
        private readonly List<Manifest> _manifests = new List<Manifest>();

        public IEnumerable<Type> HostSingletonTypes => _hostSingletonTypes;

        public string PlanarHostname { get; internal set; } = DefaultPlanarHostname;

        public int PlanarPort { get; internal set; } = DefaultPlanarPort;

        public int PlanarApiPort { get; internal set; } = DefaultPlanarApiPort;
        public HttpSchema PlanarApiSchema { get; internal set; } = HttpSchema.Http;

#if NETSTANDARD2_0

        public IHost Host { get; internal set; }
        public string PlanarApiUsername { get; internal set; }
        public string PlanarApiPassword { get; internal set; }
#else
        public IHost Host { get; internal set; } = null!;
        public string? PlanarApiUsername { get; internal set; }
        public string? PlanarApiPassword { get; internal set; }
#endif

        public IEnumerable<Manifest> Manifests => _manifests;

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

        internal void AddManifests(IEnumerable<Manifest> manifests)
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

    public class Manifest
    {
        public Manifest(string name, string content)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Name { get; }
        public string Content { get; }
    }

    public enum HttpSchema
    {
        Http,
        Https
    }
}