using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Planar.Startup
{
    public static class DataFolderInitializer
    {
        public static void CreateFolderAndFiles()
        {
            var assembly = Assembly.GetExecutingAssembly();

            var resources =
                assembly
                .GetManifestResourceNames()
                .Where(r => r.StartsWith($"{nameof(Planar)}.Data"))
                .Select(r => new { ResourceName = r, FileInfo = ConvertResourceToPath(r) })
                .ToList();

            // create folders
            resources.ForEach(r =>
            {
                if (!r.FileInfo.Directory.Exists)
                {
                    r.FileInfo.Directory.Create();
                }
            });

            // create files
            Parallel.ForEach(resources, async source =>
            {
#if DEBUG
                if (source.FileInfo.Exists)
                {
                    source.FileInfo.Delete();
                }
#endif

                if (!source.FileInfo.Exists)
                {
                    using var stream = assembly.GetManifestResourceStream(source.ResourceName);
                    using var reader = new StreamReader(stream);
                    var content = reader.ReadToEnd();
                    await File.WriteAllTextAsync(source.FileInfo.FullName, content);
                }
            });
        }

        private static readonly string[] sufixes = new[] { "json", "md", "ps1", "yml" };

        private static FileInfo ConvertResourceToPath(string resource)
        {
            var parts = resource.Split('.');
            parts = parts[1..];

            var sufix = parts.Last();
            if (sufixes.Contains(sufix))
            {
                parts = parts[..^1];
                var last = parts.Last();
                parts[^1] = $"{last}.{sufix}";
            }

            var path = Path.Combine(parts);
            var result = FolderConsts.GetPath(path);
            return new FileInfo(result);
        }
    }
}