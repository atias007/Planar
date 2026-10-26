using Microsoft.Extensions.Hosting;
using Org.BouncyCastle.Utilities;
using Planar.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Job
{
    public abstract class BaseHostedJobStartPropertiesBuilder : BaseJobStartPropertiesBuilder
    {
        public static void WithApplicationHost(PlanarHostedJobStartProperties properties, IHost host)
        {
            properties.Host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public static void WithPlanarHost(
            PlanarHostedJobStartProperties properties,
            string hostName = "localhost",
            int port = PlanarHostedJobStartProperties.DefaultPlanarPort)
        {
            if (string.IsNullOrWhiteSpace(hostName)) { throw new ArgumentNullException(nameof(hostName)); }
            if (port < 0 || port > 65_535) { throw new ArgumentException($"Port {port} is invalid"); }

            properties.PlanarPort = port;
            properties.PlanarHostname = hostName;
        }

        public static void WithPlanarApi(PlanarHostedJobStartProperties properties,
            HttpSchema schema = HttpSchema.Http,
            int port = PlanarHostedJobStartProperties.DefaultPlanarApiPort)
        {
            if (port < 0 || port > 65_535) { throw new ArgumentException($"Port {port} is invalid"); }
            properties.PlanarApiPort = port;
            properties.PlanarApiSchema = schema;
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

            var bag = new ConcurrentBag<Manifest>();
            Parallel.ForEach(files, file =>
            {
                var content = File.ReadAllText(file);
                var name = Path.Combine(folderPath, new FileInfo(file).Name);
                var yml = YmlUtil.AddSourceFilenameToManifestYml(content, name);
                bag.Add(new Manifest(name, yml));
            });

            properties.AddManifests(bag);
        }

        public static void ApplyManifestFromAssembly<T>(PlanarHostedJobStartProperties properties)
        {
            ApplyManifestFromAssembly(properties, typeof(T).Assembly);
        }

        public static void ApplyManifestFromAssembly(PlanarHostedJobStartProperties properties, params System.Reflection.Assembly[] assemblies)
        {
            if (assemblies == null) { throw new ArgumentNullException(nameof(assemblies)); }
            var yamls = new List<Manifest>();

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
                            var content = reader.ReadToEnd();
                            var yml = YmlUtil.AddSourceFilenameToManifestYml(content, resourceName);
                            var manifest = new Manifest(resourceName, yml);
                            yamls.Add(manifest);
                        }
                    }
                }
            }

            properties.AddManifests(yamls);
        }

        protected static void Validate(PlanarHostedJobStartProperties properties)
        {
            if (string.IsNullOrWhiteSpace(properties.PlanarHostname))
            {
                throw new InvalidOperationException("Planar hostname must be set. Use WithHostName(string hostName) to set the hostname.");
            }

            if (properties.HostSingletonTypes.Any() && properties.Host == null)
            {
                throw new InvalidOperationException("Host must be set if host singleton types are specified. Use WithHost(IHost host) to set the host.");
            }
        }
    }
}