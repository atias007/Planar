using BlinkSyncLib;
using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using Polly;
using Polly.Retry;
using System.IO;
using System.Text.RegularExpressions;

namespace FolderSync;

internal partial class Job : BaseCheckJob
{
#pragma warning disable S3251 // Implementations should be provided for "partial" methods

    static partial void CustomConfigure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context);

    static partial void VetoFolder(FolderSync folder);

    static partial void VetoHost(Host host);

#pragma warning restore S3251 // Implementations should be provided for "partial" methods

    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        => CustomConfigure(configurationBuilder, context);

    public async override Task ExecuteJob(IJobExecutionContext context)
    {
        Initialize(ServiceProvider);

        var defaults = GetDefaults(Configuration);
        var hosts = GetHosts(Configuration, h => VetoHost(h));
        var folders = GetFolderSyncs(Configuration, defaults);

        if (folders.Exists(e => e.IsRelativePath))
        {
            ValidateRequired(hosts, "hosts");
        }

        folders = GetFoldersWithHost(folders, hosts);
        EffectedRows = 0;
        await SafeInvokeOperation(folders, InvokeFolderSyncInnerAsync);

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

    private static List<FolderSync> GetFoldersWithHost(List<FolderSync> folders, IReadOnlyDictionary<string, HostsConfig> hosts)
    {
        var absolute = folders.Where(e => e.IsAbsolutePath);
        var relative = folders.Where(e => e.IsRelativePath);
        var result = new List<FolderSync>(absolute);
        if (relative.Any() && hosts.Count != 0)
        {
            foreach (var rel in relative)
            {
                if (!hosts.TryGetValue(rel.HostGroupName ?? string.Empty, out var hostGroup)) { continue; }
                foreach (var host in hostGroup.Hosts)
                {
                    var clone = new FolderSync(rel)
                    {
                        Host = host
                    };
                    result.Add(clone);
                }
            }
        }

        return result;
    }

    private List<FolderSync> GetFolderSyncs(IConfiguration configuration, Defaults defaults)
    {
        var result = new List<FolderSync>();
        var folders = configuration.GetRequiredSection("folders");
        foreach (var item in folders.GetChildren())
        {
            var folder = new FolderSync(item, defaults);

            VetoFolder(folder);
            if (CheckVeto(folder, "folder")) { continue; }

            ValidateFolder(folder);
            result.Add(folder);
        }

        ValidateRequired(result, "folders");
        ValidateDuplicateNames(result, "folders");

        return result;
    }

    private async Task InvokeFolderSyncInnerAsync(FolderSync folder)
    {
        await Task.Run(() => InvokeFoldersSyncInner(folder));
    }

    private void InvokeFoldersSyncInner(FolderSync folder)
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
        try
        {
            sync.Operation += SyncOperation;
            var result = sync.Start();
            PrintSummary(result);
        }
        finally
        {
            sync.Operation -= SyncOperation;
        }
    }

    private void SyncOperation(object? sender, OperationEventArgs e)
    {
        EffectedRows = EffectedRows + 1;
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

    private static void ValidateFolder(FolderSync folder)
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
    }
}