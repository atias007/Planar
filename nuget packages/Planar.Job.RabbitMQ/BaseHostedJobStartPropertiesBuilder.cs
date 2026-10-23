using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Job
{
    public abstract class BaseHostedJobStartPropertiesBuilder : BaseJobStartPropertiesBuilder
    {
        public static void WithHost(PlanarHostedJobStartProperties properties, IHost host)
        {
            properties.Host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public static void WithPlanarHostName(PlanarHostedJobStartProperties properties, string hostName)
        {
            if (string.IsNullOrWhiteSpace(hostName)) { throw new ArgumentNullException(nameof(hostName)); }
            properties.PlanarHostname = hostName;
        }

        public static void WithPlanarPorts(PlanarHostedJobStartProperties properties,
            int port = PlanarHostedJobStartProperties.DefaultPlanarPort,
            int apiPort = PlanarHostedJobStartProperties.DefaultPlanarApiPort)
        {
            if (port < 0 || port > 65_535) { throw new ArgumentException($"Port {port} is invalid"); }
            if (apiPort < 0 || apiPort > 65_535) { throw new ArgumentException($"API Port {apiPort} is invalid"); }
            properties.PlanarPort = port;
            properties.PlanarApiPort = apiPort;
        }

#if NETSTANDARD2_0

        public static void AddHostSingletonType<T>(PlanarHostedJobStartProperties properties)
#else

        public static void AddHostSingletonType<T>(PlanarHostedJobStartProperties properties) where T : notnull
#endif
        {
            properties.AddHostSingletonType<T>();
        }

        public static void ApplyManifestFromFolder(PlanarHostedJobStartProperties properties, string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath)) { throw new ArgumentNullException(nameof(folderPath)); }
            if (!Directory.Exists(folderPath)) { throw new DirectoryNotFoundException($"Folder '{folderPath}' does not exist"); }
            var files = Directory.GetFiles(folderPath, "*.yml", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(folderPath, "*.yaml", SearchOption.AllDirectories));

#if NETSTANDARD2_0
            var yamls = files.Select(file => File.ReadAllText(file));

#else
            var tasks = files.Select(file => File.ReadAllTextAsync(file));
            var yamls = Task.WhenAll(tasks).GetAwaiter().GetResult();
#endif

            properties.AddManifests(yamls);
        }

        public static void ApplyManifestFromAssembly<T>(PlanarHostedJobStartProperties properties)
        {
            ApplyManifestFromAssembly(properties, typeof(T).Assembly);
        }

        public static void ApplyManifestFromAssembly(PlanarHostedJobStartProperties properties, params System.Reflection.Assembly[] assemblies)
        {
            if (assemblies == null) { throw new ArgumentNullException(nameof(assemblies)); }
            var yamls = new List<string>();

            foreach (var assembly in assemblies)
            {
                if (assembly == null) { continue; }

                var resourceNames = assembly
                    .GetManifestResourceNames()
                    .Where(n => n.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) || n.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase));

                foreach (var resourceName in resourceNames)
                {
                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream == null) { continue; }
                        using (var reader = new StreamReader(stream))
                        {
                            var yaml = reader.ReadToEnd();
                            yamls.Add(yaml);
                        }
                    }
                }
            }

            properties.AddManifests(yamls);
        }
    }
}