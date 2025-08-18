using BlinkSyncLib;
using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using System.Text.RegularExpressions;

namespace FolderSync;

internal partial class Job : BaseCheckJob
{
#pragma warning disable S3251 // Implementations should be provided for "partial" methods

    partial void CustomConfigure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context);

    partial void VetoFolder(SyncFolder folder);

    partial void VetoHost(Host host);

    partial void Finalayze(FinalayzeDetails<IEnumerable<SyncFolder>> details);

#pragma warning restore S3251 // Implementations should be provided for "partial" methods

    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        => CustomConfigure(configurationBuilder, context);

    public async override Task ExecuteJob(IJobExecutionContext context)
    {
        Initialize(ServiceProvider);

        var defaults = GetDefaults(Configuration);
        var hosts = GetHosts(Configuration, h => VetoHost(h));
        var folders = GetSyncFolders(Configuration, defaults);

        if (folders.Exists(e => e.IsRelativePath))
        {
            ValidateRequired(hosts, "hosts");
        }

        folders = GetFoldersWithHost(folders, hosts);
        await SetEffectedRowsAsync(0);
        await SafeInvokeOperation(folders, InvokeSyncFoldersInner, context.TriggerDetails);

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

    private static List<SyncFolder> GetFoldersWithHost(List<SyncFolder> folders, IReadOnlyDictionary<string, HostsConfig> hosts)
    {
        var absolute = folders.Where(e => e.IsAbsolutePath);
        var relative = folders.Where(e => e.IsRelativePath);
        var result = new List<SyncFolder>(absolute);
        if (relative.Any() && hosts.Count != 0)
        {
            foreach (var rel in relative)
            {
                if (!hosts.TryGetValue(rel.HostGroupName ?? string.Empty, out var hostGroup))
                {
                    throw new InvalidDataException($"folder '{rel.Name}' has no host group name '{rel.HostGroupName}'");
                }

                foreach (var host in hostGroup.Hosts)
                {
                    var clone = new SyncFolder(rel)
                    {
                        Host = host
                    };
                    result.Add(clone);
                }
            }
        }

        return result;
    }

    private List<SyncFolder> GetSyncFolders(IConfiguration configuration, Defaults defaults)
    {
        var result = new List<SyncFolder>();
        var folders = configuration.GetRequiredSection("folders");
        foreach (var item in folders.GetChildren())
        {
            var folder = new SyncFolder(item, defaults);

            VetoFolder(folder);
            if (CheckVeto(folder, "folder")) { continue; }

            ValidateFolder(folder);
            result.Add(folder);
        }

        ValidateRequired(result, "folders");
        ValidateDuplicateNames(result, "folders");

        return result;
    }

    private void InvokeSyncFoldersInner(SyncFolder folder)
    {
        static Regex GetRegex(string pattern)
        {
            return new Regex(pattern, RegexOptions.Compiled | RegexOptions.Singleline, TimeSpan.FromSeconds(2));
        }

        var sourcePath = folder.GetFullSourcePath();
        var targetPath = folder.GetFullTargetPath();
        ValidatePathExists(sourcePath);
        ValidatePathExists(targetPath);

        var excludeFiles = folder.ExcludeSourceFiles?.Select(r => GetRegex(r));
        var excludeDirectories = folder.ExcludeSourceDirectories?.Select(r => GetRegex(r));
        var includeFiles = folder.IncludeSourceFiles?.Select(r => GetRegex(r));
        var includeDirectories = folder.IncludeSourceDirectories?.Select(r => GetRegex(r));
        var excludeDeleteDestinationFiles = folder.ExcludeDeleteTargetFiles?.Select(r => GetRegex(r));
        var excludeDeleteDestinationDirectories = folder.ExcludeDeleteTargetDirectories?.Select(r => GetRegex(r));

        var parameters = new InputParams
        {
            Logger = base.Logger,
            LogIoInformation = folder.LogIoInformation,
            StopAtFirstError = folder.StopAtFirstError,
            SourceDirectory = sourcePath,
            DestinationDirectory = targetPath,
            ExcludeFiles = excludeFiles?.ToArray(),
            DeleteExcludeDirs = excludeDeleteDestinationDirectories?.ToArray(),
            DeleteExcludeFiles = excludeDeleteDestinationFiles?.ToArray(),
            ExcludeDirs = excludeDirectories?.ToArray(),
            IncludeDirs = includeDirectories?.ToArray(),
            IncludeFiles = includeFiles?.ToArray(),
            DeleteFromDest = folder.DeleteFromTarget,
            ExcludeHidden = folder.ExcludeHidden
        };

        var sync = new BlinkSyncLib.Sync(parameters);

        sync.Operation += async (s, e) => await IncreaseEffectedRowsAsync();
        var result = sync.Start();
        PrintSummary(result);
    }

    private void PrintSummary(SyncResults result)
    {
        var line = string.Empty.PadLeft(40, '-');
#pragma warning disable CA2254 // Template should be a static expression
        Logger.LogInformation(line);
        Logger.LogInformation("files copied: {FilesCopied}", result.FilesCopied);
        Logger.LogInformation("files deleted: {FilesDeleted}", result.FilesDeleted);
        Logger.LogInformation("files ignored: {FilesIgnored}", result.FilesIgnored);
        Logger.LogInformation("files up to date: {FilesUpToDate}", result.FilesUpToDate);
        Logger.LogInformation("directories created: {DirectoriesCreated}", result.DirectoriesCreated);
        Logger.LogInformation("directories deleted: {DirectoriesDeleted}", result.DirectoriesDeleted);
        Logger.LogInformation("directories ignored: {DirectoriesIgnored}", result.DirectoriesIgnored);
        Logger.LogInformation(line);
        if (result.TotalErrors == 0)
        {
            Logger.LogInformation("total errors: {TotalErrors}", result.TotalErrors);
        }
        else
        {
            Logger.LogWarning("total errors: {TotalErrors}", result.TotalErrors);
        }

        Logger.LogInformation(line);
#pragma warning restore CA2254 // Template should be a static expression
    }

    private static void ValidateFolder(SyncFolder folder)
    {
        var section = $"folders ({folder.Name})";

        ValidateRequired(folder.Name, "name", section);
        ValidateMaxLength(folder.Name, 50, "name", section);

        ValidateRequired(folder.SourcePath, "path", section);
        ValidateMaxLength(folder.SourcePath, 1_000, "path", section);

        ValidateRequired(folder.TargetPath, "path", section);
        ValidateMaxLength(folder.TargetPath, 1_000, "path", section);

        ValidateNullOrWhiteSpace(folder.ExcludeSourceFiles, $"{section} --> exclude files");
        ValidateNullOrWhiteSpace(folder.ExcludeSourceFiles, $"{section} --> exclude directories");
        ValidateNullOrWhiteSpace(folder.ExcludeSourceFiles, $"{section} --> include files");
        ValidateNullOrWhiteSpace(folder.ExcludeSourceFiles, $"{section} --> include directories");
        ValidateNullOrWhiteSpace(folder.ExcludeSourceFiles, $"{section} --> exclude delete destination files");
        ValidateNullOrWhiteSpace(folder.ExcludeSourceFiles, $"{section} --> exclude delete destination directories");

        var hasInclude = IsNotEmpty(folder.IncludeSourceFiles) || IsNotEmpty(folder.IncludeSourceDirectories);
        var hasExclude = IsNotEmpty(folder.ExcludeSourceFiles) || IsNotEmpty(folder.ExcludeSourceDirectories);
        if (hasInclude && hasExclude)
        {
            throw new InvalidDataException($"folder '{folder.Name}' has both include and exclude files/directories");
        }

        var fullsource = Path.GetFullPath(folder.SourcePath) + Path.PathSeparator;
        var fullDestination = Path.GetFullPath(folder.TargetPath) + Path.PathSeparator;
        if (fullDestination.StartsWith(fullsource) || fullsource.StartsWith(fullDestination))
        {
            throw new InvalidDataException($"source directory {fullsource} and destination directory {fullDestination} cannot contain each other");
        }

        if ((folder.ExcludeDeleteTargetFiles != null || folder.ExcludeDeleteTargetDirectories != null) && (!folder.DeleteFromTarget))
        {
            throw new InvalidDataException("exclude from deletion files/directories options require delete from target enabled");
        }

        // ensure source directory exists
        if (!Directory.Exists(folder.SourcePath))
        {
            throw new InvalidDataException($"source directory {folder.SourcePath} not found");
        }
    }

    private static bool IsNotEmpty(IEnumerable<string>? items)
    {
        return items != null && items.Any();
    }
}