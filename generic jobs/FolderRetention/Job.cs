using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using Polly;
using Polly.Retry;

namespace FolderRetention;

internal partial class Job : BaseCheckJob
{
#pragma warning disable S3251 // Implementations should be provided for "partial" methods

    static partial void CustomConfigure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context);

    static partial void VetoFolder(ref Folder folder);

    static partial void VetoHost(ref Host host);

#pragma warning restore S3251 // Implementations should be provided for "partial" methods

    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        => CustomConfigure(configurationBuilder, context);

    private static readonly RetryPolicy _policy = Policy.Handle<Exception>()
                    .WaitAndRetry(
                        retryCount: 3,
                        sleepDurationProvider: _ => TimeSpan.FromSeconds(3));

    public async override Task ExecuteJob(IJobExecutionContext context)
    {
        Initialize(ServiceProvider);

        var hosts = GetHosts(Configuration, h => VetoHost(ref h));
        var folders = GetFolders(Configuration);

        if (folders.Exists(e => e.IsRelativePath))
        {
            ValidateRequired(hosts, "hosts");
        }

        folders = GetFoldersWithHost(folders, hosts);

        var tasks = SafeInvokeOperation(folders, InvokeFolderInnerAsync);
        await Task.WhenAll(tasks);

        Finalayze();
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.RegisterBaseCheck();
    }

    private static List<Folder> GetFoldersWithHost(List<Folder> folders, IReadOnlyDictionary<string, HostsConfig> hosts)
    {
        var absolute = folders.Where(e => e.IsAbsolutePath);
        var relative = folders.Where(e => e.IsRelativePath);
        var result = new List<Folder>(absolute);
        if (relative.Any() && hosts.Count != 0)
        {
            foreach (var rel in relative)
            {
                if (!hosts.TryGetValue(rel.HostGroupName ?? string.Empty, out var hostGroup)) { continue; }
                foreach (var host in hostGroup.Hosts)
                {
                    var clone = new Folder(rel)
                    {
                        Host = host
                    };
                    result.Add(clone);
                }
            }
        }

        return result;
    }

    private static IEnumerable<FileInfo> GetFiles(string path, Folder folder)
    {
        var fi = new DirectoryInfo(path);

        var option = folder.IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        foreach (var pattern in folder.FilesPattern!)
        {
            var files = fi.GetFiles(pattern, option);
            foreach (var file in files)
            {
                yield return file;
            }
        }
    }

    private List<Folder> GetFolders(IConfiguration configuration)
    {
        var result = new List<Folder>();
        var folders = configuration.GetRequiredSection("folders");
        foreach (var item in folders.GetChildren())
        {
            var folder = new Folder(item);

            VetoFolder(ref folder);
            if (CheckVeto(folder, "folder")) { continue; }

            ValidateFolder(folder);
            result.Add(folder);
        }

        ValidateRequired(result, "folders");
        ValidateDuplicateNames(result, "folders");

        return result;
    }

    private static void ValidateFilesPattern(Folder folder)
    {
        if (folder.FilesPattern?.Any(p => p.Length > 100) ?? false)
        {
            throw new InvalidDataException($"'monitor' on folder name '{folder.Name}' has file pattern length more then 100 chars");
        }

        if (folder.FilesPattern?.Any(string.IsNullOrWhiteSpace) ?? false)
        {
            throw new InvalidDataException($"'monitor' on folder name '{folder.Name}' has empty file pattern");
        }
    }

    private async Task InvokeFolderInnerAsync(Folder folder)
    {
        await Task.Run(() => InvokeFoldersInner(folder));
    }

    private void InvokeFoldersInner(Folder folder)
    {
        if (!folder.Active)
        {
            Logger.LogInformation("skipping inactive folder '{Name}'", folder.Name);
            return;
        }

        var path = folder.GetFullPath();
        ValidatePathExists(path);

        var files = GetFiles(path, folder);
        var count = files.Count();
        var filesToDelete = new Dictionary<FileInfo, string>();

        if (folder.FileSizeNumber != null && count > 0)
        {
            var toBeDelete = files.Where(f => f.Length > folder.FileSizeNumber).ToList();
            toBeDelete.ForEach(f => filesToDelete.Add(f, $"size {f.Length:N0} above {folder.FileSizeNumber:N0}"));
        }

        if (folder.CreatedAgeDate != null && count > 0)
        {
            var toBeDelete = files.Where(f => f.CreationTime < folder.CreatedAgeDate).ToList();
            toBeDelete.ForEach(f => filesToDelete.Add(f, $"creation date {f.CreationTime} before {folder.CreatedAgeDate}"));
        }

        if (folder.ModifiedAgeDate != null && count > 0)
        {
            var toBeDelete = files.Where(f => f.LastWriteTime < folder.ModifiedAgeDate).ToList();
            toBeDelete.ForEach(f => filesToDelete.Add(f, $"modified date {f.LastWriteTime} before {folder.ModifiedAgeDate}"));
        }

        DeleteFiles(filesToDelete, path, folder.MaxFiles);

        if (folder.DeleteEmptyDirectories)
        {
            DeleteEmptySubdirectories(path);
        }
    }

    private void DeleteFiles(Dictionary<FileInfo, string> filesToDelete, string path, int maxFails)
    {
        var fails = 0;
        var success = 0;

        foreach (var (file, reason) in filesToDelete)
        {
            try
            {
                _policy.Execute(() =>
                {
                    file.Delete();
                    Logger.LogInformation("file '{FileName}' deleted from folder '{Path}', reason: {Reason}", file.FullName, path, reason);
                });
                success++;
            }
            catch (Exception ex)
            {
                fails++;
#pragma warning disable S6667 // Logging in a catch clause should pass the caught exception as a parameter.
                Logger.LogWarning("error deleting file '{FileName}' from folder '{Path}', reason: {Reason}", file.FullName, path, ex.Message);
#pragma warning restore S6667 // Logging in a catch clause should pass the caught exception as a parameter.

                if (fails >= maxFails)
                {
                    throw new CheckException($"error deleting files from folder '{path}'", ex);
                }
            }
        }

        if (success == 0)
        {
            Logger.LogInformation("[x] no files deleted from folder '{Path}'", path);
        }
        else
        {
            Logger.LogInformation("[x] total {Count} file(s) deleted from folder '{Path}'. {Fails} fails", success, path, fails == 0 ? "no" : fails);
        }
    }

    private void DeleteFolder(string folder, string parent)
    {
        try
        {
            _policy.Execute(() =>
            {
                Directory.Delete(folder);
                Logger.LogInformation("empty directory '{Directory}' deleted from folder '{Path}'", folder, parent);
            });
        }
        catch (Exception ex)
        {
#pragma warning disable S6667 // Logging in a catch clause should pass the caught exception as a parameter.
            Logger.LogWarning("error deleting folder '{Directory}' from folder '{Path}', reason: {Reason}", folder, folder, ex.Message);
#pragma warning restore S6667 // Logging in a catch clause should pass the caught exception as a parameter.
        }
    }

    private void DeleteEmptySubdirectories(string path)
    {
        if (Directory.Exists(path))
        {
            // Enumerate all subdirectories
            foreach (string subdirectory in Directory.EnumerateDirectories(path))
            {
                // Check if the subdirectory is empty
                if (!Directory.EnumerateFileSystemEntries(subdirectory).Any())
                {
                    DeleteFolder(subdirectory, path);
                }
                else
                {
                    // Recursively call for subdirectories (optional)
                    DeleteEmptySubdirectories(subdirectory);
                }
            }
        }
        else
        {
            Console.WriteLine($"Directory not found: {path}");
        }
    }

    private static void ValidateFolder(Folder folder)
    {
        ValidateRequired(folder.Name, "name", "folders");
        ValidateMaxLength(folder.Name, 50, "name", "folders");

        var section = $"folders ({folder.Name})";
        ValidateRequired(folder.Path, "path", "folders");
        ValidateMaxLength(folder.Path, 1_000, "path", "folders");

        ValidateGreaterThenOrEquals(folder.MaxFiles, 0, "max files", section);

        ValidateGreaterThen(folder.FileSizeNumber, 0, "file size", section);
        ValidateFilesPattern(folder);

        if (!folder.IsValid())
        {
            throw new InvalidDataException($"folder '{folder.Name}' has no arguments to check");
        }
    }
}