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
            var names = assembly.GetManifestResourceNames();

            var resources =
                names
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

        private static FileInfo ConvertResourceToPath(string resource)
        {
            var parts = resource.Split('.');
            var fileParts = parts[3..];
            var pathParts = parts[1..3];
            var filename = string.Join(".", fileParts);
            var path = Path.Combine(pathParts);
            var fullname = Path.Combine(path, filename);
            var result = FolderConsts.GetPath(fullname);
            return new FileInfo(result);
        }
    }
}