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

    partial void CustomConfigure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context);

    partial void VetoFolder(Folder folder);

    partial void VetoHost(Host host);

    partial void Finalayze(FinalayzeDetails<IEnumerable<Folder>> details);

#pragma warning restore S3251 // Implementations should be provided for "partial" methods

    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        => CustomConfigure(configurationBuilder, context);

    private static readonly RetryPolicy _policy = Policy.Handle<Exception>()
                    .WaitAndRetry(
                        retryCount: 3,
                        sleepDurationProvider: _ => TimeSpan.FromSeconds(3));

    private int fails;

    public async override Task ExecuteJob(IJobExecutionContext context)
    {
        Initialize(ServiceProvider);

        var defaults = GetDefaults(Configuration);
        var hosts = GetHosts(Configuration, h => VetoHost(h));
        var folders = GetFolders(Configuration, defaults);

        if (folders.Exists(e => e.IsRelativePath))
        {
            ValidateRequired(hosts, "hosts");
        }

        folders = GetFoldersWithHost(folders, hosts);
        EffectedRows = 0;
        await SafeInvokeOperation(folders, InvokeFoldersInner, context.TriggerDetails);

        var details = GetFinalayzeDetails(folders.AsEnumerable());
        Finalayze(details);
        Finalayze();
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.RegisterSpanCheck();
    }

    private Defaults GetDefaults(IConfiguration configuration)
    {
        var empty = Defaults.Empty;
        var section = configuration.GetSection("defaults");
        if (section == null)
        {
            Logger.LogWarning("no defaults section found on settings file. set job factory defaults");
            return empty;
        }

        var result = new Defaults(section);
        ValidateBase(result, "defaults");

        return result;
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
        var option = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = folder.IncludeSubdirectories
        };

        foreach (var pattern in folder.FilesPattern!)
        {
            var files = Directory.EnumerateFiles(path, pattern, option);
            foreach (var file in files)
            {
                yield return new FileInfo(file);
            }
        }
    }

    private List<Folder> GetFolders(IConfiguration configuration, Defaults defaults)
    {
        var result = new List<Folder>();
        var folders = configuration.GetRequiredSection("folders");
        foreach (var item in folders.GetChildren())
        {
            var folder = new Folder(item, defaults);

            VetoFolder(folder);
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

    private void InvokeFoldersInner(Folder folder)
    {
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
            DeleteEmptySubdirectories(path, folder.MaxFiles);
        }
    }

    private void DeleteFiles(Dictionary<FileInfo, string> filesToDelete, string path, int maxFails)
    {
        var success = 0;

        Parallel.ForEach(filesToDelete, kv =>
        {
            var file = kv.Key;
            try
            {
                _policy.Execute(() =>
                {
                    file.Delete();
                    Logger.LogInformation("file '{FileName}' deleted from folder '{Path}', reason: {Reason}", file.FullName, path, kv.Value);
                    EffectedRows++;
                });
                Interlocked.Increment(ref success);
            }
            catch (Exception ex)
            {
                var value = Interlocked.Increment(ref fails);
#pragma warning disable S6667 // Logging in a catch clause should pass the caught exception as a parameter.
                Logger.LogWarning("error deleting file '{FileName}' from folder '{Path}', reason: {Reason}", file.FullName, path, ex.Message);
#pragma warning restore S6667 // Logging in a catch clause should pass the caught exception as a parameter.

                if (value >= maxFails)
                {
                    throw new CheckException($"error deleting files from folder '{path}'", ex);
                }
            }
        });

#pragma warning disable S2583 // Conditionally executed code should be reachable
        if (success == 0)
        {
            Logger.LogInformation("[x] no files deleted from folder '{Path}'", path);
        }
        else
        {
            Logger.LogInformation("[x] total {Count} file(s) deleted from folder '{Path}'. {Fails} fails", success, path, fails == 0 ? "no" : fails);
        }
#pragma warning restore S2583 // Conditionally executed code should be reachable
    }

    private void DeleteFolder(string path, string subdirectory)
    {
        _policy.Execute(() =>
        {
            Directory.Delete(subdirectory);
            Logger.LogInformation("empty directory '{Directory}' deleted from folder '{Path}'", subdirectory, path);
        });
    }

    private void DeleteEmptySubdirectoriesInner(string path, string subdirectory, int maxFails)
    {
        try
        {
            if (!Directory.Exists(subdirectory)) { return; }

            DeleteEmptySubdirectories(subdirectory, maxFails);

            if (!Directory.EnumerateFileSystemEntries(subdirectory).Any())
            {
                DeleteFolder(path, subdirectory);
            }
        }
        catch (Exception ex)
        {
            var value = Interlocked.Increment(ref fails);
#pragma warning disable S6667 // Logging in a catch clause should pass the caught exception as a parameter.
            Logger.LogWarning("error deleting folder '{Directory}' from folder '{Path}', reason: {Reason}", subdirectory, path, ex.Message);
#pragma warning restore S6667 // Logging in a catch clause should pass the caught exception as a parameter.
            if (value >= maxFails)
            {
                throw new CheckException($"error deleting empty folder '{subdirectory}'", ex);
            }
        }
    }

    private void DeleteEmptySubdirectories(string path, int maxFails)
    {
        if (!Directory.Exists(path)) { return; }

        var subdirs = Directory.EnumerateDirectories(path);
        Parallel.ForEach(subdirs, subdirectory =>
        {
            DeleteEmptySubdirectoriesInner(path, subdirectory, maxFails);
        });
    }

    private static void ValidateFolder(Folder folder)
    {
        var section = $"folders ({folder.Name})";

        ValidateRequired(folder.Name, "name", section);
        ValidateMaxLength(folder.Name, 50, "name", section);

        ValidateRequired(folder.Path, "path", section);
        ValidateMaxLength(folder.Path, 1_000, "path", section);

        ValidateGreaterThenOrEquals(folder.MaxFiles, 0, "max files", section);

        ValidateGreaterThen(folder.FileSizeNumber, 0, "file size", section);
        ValidateFilesPattern(folder);

        if (!folder.IsValid())
        {
            throw new InvalidDataException($"folder '{folder.Name}' has no arguments to check");
        }
    }
}