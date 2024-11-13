using Planar.Service.General;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Twilio.Base;

namespace Planar.Startup
{
    public static class DataFolderInitializer
    {
        public static void CreateFolderAndFiles()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var names = assembly.GetManifestResourceNames();

            _ = CreateJobFiles();

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

        private static async Task CreateJobFiles()
        {
            static void EnsurePath(string path)
            {
                if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
            }

            string jobFileFolder;
            try
            {
                var root = FolderConsts.GetDataFolder(fullPath: true);
                jobFileFolder = Path.Combine(root, "JobFiles");
                EnsurePath(jobFileFolder);
            }
            catch
            {
                // *** DO NOTHING *** //
                return;
            }

            var types = ServiceUtil.JobTypes;
            foreach (var t in types)
            {
                try
                {
                    var path = Path.Combine(jobFileFolder, t);
                    EnsurePath(path);

                    var assembly = Assembly.Load(t);
                    var resource = $"{t}.JobFile.yml";
                    using var stream = assembly.GetManifestResourceStream(resource);
                    using var reader = new StreamReader(stream);
                    var content = await reader.ReadToEndAsync();
                    await File.WriteAllTextAsync(Path.Combine(path, "JobFile.yml"), content);
                }
                catch
                {
                    // *** DO NOTHING *** //
                }
            }
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