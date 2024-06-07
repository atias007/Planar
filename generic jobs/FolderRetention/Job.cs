using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using Polly;
using Polly.Retry;
using System.IO;

namespace FolderRetention;

internal class Job : BaseCheckJob
{
    private static readonly RetryPolicy _policy = Policy.Handle<Exception>()
                    .WaitAndRetry(
                        retryCount: 3,
                        sleepDurationProvider: _ => TimeSpan.FromSeconds(3));

    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
    }

    public async override Task ExecuteJob(IJobExecutionContext context)
    {
        var hosts = GetHosts(Configuration);
        var folders = GetFolders(Configuration);

        if (!hosts.Any() && folders.Exists(e => !e.IsAbsolutePath))
        {
            throw new InvalidDataException("no hosts defined and at least one folder path is relative");
        }

        folders.ForEach(f => ValidateFolderExists(f, hosts));

        var tasks = SafeInvokeOperation(folders, f => InvokeFolderInnerAsync(f, hosts));
        await Task.WhenAll(tasks);

        CheckAggragateException();
        HandleCheckExceptions();
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.RegisterBaseCheck();
    }

    private static IEnumerable<FileInfo> GetFiles(string path, Folder folder)
    {
        var fi = new DirectoryInfo(path);
        if (folder.FilesPattern == null || !folder.FilesPattern.Any()) { folder.SetDefaultFilePattern(); }
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

    private static List<Folder> GetFolders(IConfiguration configuration)
    {
        var result = new List<Folder>();
        var folders = configuration.GetRequiredSection("folders");
        foreach (var item in folders.GetChildren())
        {
            var folder = new Folder(item);
            folder.SetFolderArguments();
            folder.IsAbsolutePath = Path.IsPathFullyQualified(folder.Path);
            ValidateFolder(folder);
            result.Add(folder);
        }

        ValidateRequired(result, "folders");
        ValidateDuplicateNames(result, "folders");

        return result;
    }

    private static IEnumerable<string> GetHosts(IConfiguration configuration)
    {
        var hosts = configuration.GetSection("hosts");
        if (hosts == null) { return []; }
        var result = hosts.Get<string[]>() ?? [];
        return result.Distinct();
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

    private static void ValidatePathExists(string path)
    {
        try
        {
            var directory = new DirectoryInfo(path);
            if (!directory.Exists)
            {
                throw new InvalidDataException($"directory '{path}' not found");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"directory '{path}' is invalid ({ex.Message})");
        }
    }

    private async Task InvokeFolderInnerAsync(Folder folder, IEnumerable<string> hosts)
    {
        await Task.Run(() => InvokeFoldersInner(folder, hosts));
    }

    private void InvokeFoldersInner(Folder folder, IEnumerable<string> hosts)
    {
        if (folder.IsAbsolutePath)
        {
            InvokeFolderInner(folder, null);
        }
        else
        {
            Parallel.ForEach(hosts, host => InvokeFolderInner(folder, host));
        }
    }

    private void InvokeFolderInner(Folder folder, string? host)
    {
        if (!folder.Active)
        {
            Logger.LogInformation("skipping inactive folder '{Name}'", folder.Name);
            return;
        }

        var path = folder.GetFullPath(host);
        var files = GetFiles(path, folder);
        var filesToDelete = new Dictionary<FileInfo, string>();

        if (folder.FileSizeNumber != null)
        {
            var toBeDelete = files.Where(f => f.Length > folder.FileSizeNumber).ToList();
            toBeDelete.ForEach(f => filesToDelete.Add(f, $"size {f.Length:N0} above {folder.FileSizeNumber:N0}"));
        }

        if (folder.CreatedAgeDate != null)
        {
            var toBeDelete = files.Where(f => f.CreationTime < folder.CreatedAgeDate).ToList();
            toBeDelete.ForEach(f => filesToDelete.Add(f, $"creation date {f.CreationTime} before {folder.CreatedAgeDate}"));
        }

        if (folder.ModifiedAgeDate != null)
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

    private static void ValidateFolderExists(Folder folder, IEnumerable<string> hosts)
    {
        foreach (var host in hosts)
        {
            var path = folder.GetFullPath(host);
            ValidatePathExists(path);
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