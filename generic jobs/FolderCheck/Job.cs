using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using Polly;

namespace FolderCheck;

internal class Job : BaseCheckJob
{
    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
    }

    public async override Task ExecuteJob(IJobExecutionContext context)
    {
        var tasks = new List<Task>();
        var defaults = GetDefaults(Configuration);
        var hosts = GetHosts(Configuration);
        var folders = GetFolders(Configuration, hosts);
        ValidateFolders(folders);
        CheckAggragateException();

        foreach (var f in folders)
        {
            FillDefaults(f, defaults);
            if (!ValidateFolder(f)) { continue; }
            var task = Task.Run(() => SafeInvokeFolder(f));
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        CheckAggragateException();
        HandleCheckExceptions("folder");
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.AddSingleton<CheckFailCounter>();
    }

    private static void FillDefaults(Folder folder, Defaults defaults)
    {
        SetDefaultName(folder, () => folder.Name);
        FillBase(folder, defaults);
    }

    private static IEnumerable<FileInfo> GetFiles(Folder folder)
    {
        var fi = new DirectoryInfo(folder.Path);
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

    private static IEnumerable<Folder> GetFolders(IConfiguration configuration, IEnumerable<string> hosts)
    {
        const string hostPlaceholder = "{{host}}";

        var folders = configuration.GetRequiredSection("folders");
        foreach (var item in folders.GetChildren())
        {
            var path = item.GetValue<string>("path") ?? string.Empty;
            if (path.Contains(hostPlaceholder))
            {
                foreach (var host in hosts)
                {
                    var path2 = path.Replace(hostPlaceholder, host);
                    var folder = new Folder(item, path2);
                    yield return folder;
                }
            }
            else
            {
                var folder = new Folder(item, path);
                yield return folder;
            }
        }
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

    private static void ValidatePathExists(Folder folder)
    {
        try
        {
            var directory = new DirectoryInfo(folder.Path);
            if (!directory.Exists)
            {
                throw new InvalidDataException($"directory '{folder.Path}' not found");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"directory '{folder.Path}' is invalid ({ex.Message})");
        }
    }

    private Defaults GetDefaults(IConfiguration configuration)
    {
        var empty = Defaults.Empty;
        var defaults = configuration.GetSection("defaults");
        if (defaults == null)
        {
            Logger.LogWarning("no defaults section found on settings file. set job factory defaults");
            return empty;
        }

        var result = new Defaults
        {
            RetryCount = defaults.GetValue<int?>("retry count") ?? empty.RetryCount,
            RetryInterval = defaults.GetValue<TimeSpan?>("retry interval") ?? empty.RetryInterval,
            MaximumFailsInRow = defaults.GetValue<int?>("maximum fails in row") ?? empty.MaximumFailsInRow,
        };

        ValidateBase(result, "defaults");

        return result;
    }

    private void InvokeFolderInner(Folder folder)
    {
        var files = GetFiles(folder);
        if (folder.TotalSizeNumber != null)
        {
            var size = files.Sum(f => f.Length);
            Logger.LogInformation("path {Path} size is {Size:N0} byte(s)", folder.Path, size);
            if (size > folder.FileSizeNumber)
            {
                throw new CheckException($"folder '{folder.Path}' size is greater then {folder.TotalSizeNumber:N0}", folder.Name);
            }
        }

        if (folder.FileSizeNumber != null)
        {
            var max = files.Max(f => f.Length);
            Logger.LogInformation("path {Path} max file size is {Size:N0} byte(s)", folder.Path, max);
            if (max > folder.FileSizeNumber)
            {
                throw new CheckException($"folder '{folder.Path}' has file size that is greater then {folder.FileSizeNumber:N0}", folder.Name);
            }
        }

        if (folder.FileCount != null)
        {
            var count = files.Count();
            Logger.LogInformation("path {Path} contains {Count:N0} file(s)", folder.Path, count);
            if (count > folder.FileCount)
            {
                throw new CheckException($"folder '{folder.Path}' contains more then {folder.FileCount:N0} files", folder.Name);
            }
        }

        if (folder.CreatedAgeDate != null)
        {
            var created = files.Min(f => f.CreationTimeUtc);
            Logger.LogInformation("path {Path} most old created file is {Created}", folder.Path, created);
            if (created < folder.CreatedAgeDate)
            {
                throw new CheckException($"folder '{folder.Path}' contains files that are created older then {folder.CreatedAge}", folder.Name);
            }
        }

        if (folder.ModifiedAgeDate != null)
        {
            var modified = files.Min(f => f.LastWriteTimeUtc);
            Logger.LogInformation("path {Path} most old modified file is {Created}", folder.Path, modified);
            if (modified < folder.ModifiedAgeDate)
            {
                throw new CheckException($"folder '{folder.Path}' contains files that are modified older then {folder.ModifiedAgeDate}", folder.Name);
            }
        }

        Logger.LogInformation("folder check success, folder '{FolderName}', path '{FolderPath}'",
                        folder.Name, folder.Path);
    }

    private void SafeHandleException(Folder folder, Exception ex, CheckFailCounter counter)
    {
        try
        {
            var exception = ex is CheckException ? null : ex;

            if (exception == null)
            {
                Logger.LogError("folder check fail for folder name '{FolderName}' with path '{FolderPath}'. reason: {Message}",
                  folder.Name, folder.Path, ex.Message);
            }
            else
            {
                Logger.LogError(exception, "folder check fail for folder name '{FolderName}' with path '{FolderPath}'. reason: {Message}",
                    folder.Name, folder.Path, ex.Message);
            }

            var value = counter.IncrementFailCount(folder);

            if (value > folder.MaximumFailsInRow)
            {
                Logger.LogWarning("folder check fail for folder name '{FolderName}' with path '{FolderPath}' but maximum fails in row reached. reason: {Message}",
                    folder.Name, folder.Path, ex.Message);
            }
            else
            {
                var hcException = new CheckException(
                    $"folder check fail for folder name '{folder.Name}' with path '{folder.Path}'. reason: {ex.Message}",
                    folder.Name);

                AddCheckException(hcException);
            }
        }
        catch (Exception innerEx)
        {
            AddAggregateException(innerEx);
        }
    }

    private void SafeInvokeFolder(Folder folder)
    {
        var counter = ServiceProvider.GetRequiredService<CheckFailCounter>();

        try
        {
            if (folder.RetryCount == 0)
            {
                InvokeFolderInner(folder);
                return;
            }

            Policy.Handle<Exception>()
                    .WaitAndRetry(
                        retryCount: folder.RetryCount.GetValueOrDefault(),
                        sleepDurationProvider: _ => folder.RetryInterval.GetValueOrDefault(),
                         onRetry: (ex, _) =>
                         {
                             var exception = ex is CheckException ? null : ex;
                             Logger.LogWarning(exception, "retry for folder name '{FolderName}' with path '{FolderPath}'. Reason: {Message}",
                                                                     folder.Name, folder.Path, ex.Message);
                         })
                    .Execute(() =>
                    {
                        InvokeFolderInner(folder);
                    });

            counter.ResetFailCount(folder);
        }
        catch (Exception ex)
        {
            SafeHandleException(folder, ex, counter);
        }
    }

    private bool ValidateFolder(Folder folder)
    {
        try
        {
            ValidateMaxLength(folder.Name, 50, "name", "folders");

            var section = $"folders ({folder.Name})";
            ValidateBase(folder, section);
            ValidateRequired(folder.Path, "path", "folders");
            ValidateMaxLength(folder.Path, 1000, "path", "folders");
            ValidatePathExists(folder);

            folder.SetMonitorArguments();
            ValidateGreaterThen(folder.TotalSizeNumber, 0, "total size", section);
            ValidateGreaterThen(folder.FileSizeNumber, 0, "file size", section);
            ValidateGreaterThen(folder.FileCount, 0, "file count", section);
            ValidateFilesPattern(folder);

            if (!folder.IsValid())
            {
                throw new InvalidDataException($"folder '{folder.Name}' has no arguments to check");
            }
        }
        catch (Exception ex)
        {
            AddAggregateException(ex);
            return false;
        }

        return true;
    }

    private void ValidateFolders(IEnumerable<Folder> folders)
    {
        try
        {
            CommonUtil.ValidateItems(folders, "folders", "path");
        }
        catch (Exception ex)
        {
            AddAggregateException(ex);
        }
    }
}