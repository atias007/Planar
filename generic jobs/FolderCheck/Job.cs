using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;

namespace FolderCheck;

internal class Job : BaseCheckJob
{
    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
    }

    public async override Task ExecuteJob(IJobExecutionContext context)
    {
        Initialize(ServiceProvider);

        var defaults = GetDefaults(Configuration);
        var hosts = GetHosts(Configuration);
        var folders = GetFolders(Configuration, defaults);

        if (!hosts.Any() && folders.Exists(e => e.IsRelativePath))
        {
            throw new InvalidDataException("no hosts defined and at least one folder path is relative");
        }

        folders.ForEach(f => ValidateFolderExists(f, hosts));

        EffectedRows = 0;

        await SafeInvokeCheck(folders, f => InvokeFolderInnerAsync(f, hosts));

        Finilayze();
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

    private static List<Folder> GetFolders(IConfiguration configuration, Defaults defaults)
    {
        var result = new List<Folder>();
        var folders = configuration.GetRequiredSection("folders");
        foreach (var item in folders.GetChildren())
        {
            var folder = new Folder(item, defaults);
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

    private Defaults GetDefaults(IConfiguration configuration)
    {
        var empty = Defaults.Empty;
        var defaults = configuration.GetSection("defaults");
        if (defaults == null)
        {
            Logger.LogWarning("no defaults section found on settings file. set job factory defaults");
            return empty;
        }

        var result = new Defaults(defaults);
        ValidateBase(result, "defaults");

        return result;
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
        if (folder.TotalSizeNumber != null)
        {
            var size = files.Sum(f => f.Length);
            Logger.LogInformation("folder '{Path}' size is {Size:N0} byte(s)", path, size);
            if (size > folder.FileSizeNumber)
            {
                throw new CheckException($"folder '{path}' size is greater then {folder.TotalSizeNumber:N0}");
            }
        }

        if (folder.FileSizeNumber != null)
        {
            var max = files.Max(f => f.Length);
            Logger.LogInformation("folder '{Path}' max file size is {Size:N0} byte(s)", path, max);
            if (max > folder.FileSizeNumber)
            {
                throw new CheckException($"folder '{path}' has file size that is greater then {folder.FileSizeNumber:N0}");
            }
        }

        if (folder.FileCount != null)
        {
            var count = files.Count();
            Logger.LogInformation("folder '{Path}' contains {Count:N0} file(s)", path, count);
            if (count > folder.FileCount)
            {
                throw new CheckException($"folder '{path}' contains more then {folder.FileCount:N0} files");
            }
        }

        if (folder.CreatedAgeDate != null)
        {
            var created = files.Min(f => f.CreationTime);
            Logger.LogInformation("folder '{Path}' most old created file is {Created}", path, created);
            if (created < folder.CreatedAgeDate)
            {
                throw new CheckException($"folder '{path}' contains files that are created older then {folder.CreatedAge}");
            }
        }

        if (folder.ModifiedAgeDate != null)
        {
            var modified = files.Min(f => f.LastWriteTime);
            Logger.LogInformation("folder '{Path}' most old modified file is {Created}", path, modified);
            if (modified < folder.ModifiedAgeDate)
            {
                throw new CheckException($"folder '{path}' contains files that are modified older then {folder.ModifiedAgeDate}");
            }
        }

        Logger.LogInformation("folder check success, folder '{FolderName}', path '{FolderPath}'",
                        folder.Name, path);

        IncreaseEffectedRows();
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
        ValidateMaxLength(folder.Path, 1000, "path", "folders");

        ValidateBase(folder, section);
        ValidateGreaterThen(folder.TotalSizeNumber, 0, "total size", section);
        ValidateGreaterThen(folder.FileSizeNumber, 0, "file size", section);
        ValidateGreaterThen(folder.FileCount, 0, "file count", section);
        ValidateFilesPattern(folder);

        if (!folder.IsValid)
        {
            throw new InvalidDataException($"folder '{folder.Name}' has no arguments to check");
        }
    }
}