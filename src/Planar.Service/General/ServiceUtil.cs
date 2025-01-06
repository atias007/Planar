using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Common.Exceptions;
using Planar.Hooks;
using Planar.Service.Monitor;
using Quartz;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Planar.Service.General;

public static class ServiceUtil
{
    internal static ConcurrentDictionary<string, HookWrapper> MonitorHooks { get; private set; } = new();
    private static bool _disposeFlag;
    private static readonly object _locker = new();

    public static IEnumerable<string> JobTypes =>
    [
        nameof(PlanarJob),
        nameof(ProcessJob),
        nameof(SqlJob),
        nameof(RestJob),
        nameof(SqlTableReportJob),
        nameof(WorkflowJob),
    ];

    internal static string GenerateId()
    {
        var id = Path.GetRandomFileName().Replace(".", string.Empty);
        return id;
    }

    public static IEnumerable<string> SearchNewHooks()
    {
        var path = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.MonitorHooks);
        if (!Directory.Exists(path)) { return []; }
        var files = Directory.GetFiles(path, "*.exe", SearchOption.AllDirectories);
        var result = files.Select(f => f[(path.Length + 1)..]);
        return result;
    }

    public static void LoadMonitorHooks<T>(IEnumerable<MonitorHookDetails> hooks, ILogger<T> logger)
    {
        MonitorHooks.Clear();
        LoadSystemHooks(logger);

        foreach (var item in hooks)
        {
            LoadHook(logger, item);
        }
    }

    private static void LoadHook<T>(ILogger<T> logger, MonitorHookDetails hookDetails)
    {
        var path = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.MonitorHooks);
        var filename = Path.Combine(path, hookDetails.Path);
        var wrapper = HookWrapper.CreateExternal(filename, hookDetails, logger);
        var result = MonitorHooks.TryAdd(hookDetails.Name, wrapper);

        if (result)
        {
            logger.LogInformation("add monitor hook {Name} from file {Filename}", hookDetails.Name, filename);
        }
        else
        {
            logger.LogError("fail to add monitor hook {Name} from file {Filename}. already contains monitor with this name", hookDetails.Name, filename);
        }
    }

    private static void LoadSystemHooks<TLogger>(ILogger<TLogger> logger)
    {
        LoadSystemHook<TLogger, PlanarRestHook>(logger);
        LoadSystemHook<TLogger, PlanarSmtpHook>(logger);
        LoadSystemHook<TLogger, PlanarLogHook>(logger);
        LoadSystemHook<TLogger, PlanarTeamsHook>(logger);
        LoadSystemHook<TLogger, PlanarTwilioSmsHook>(logger);
        LoadSystemHook<TLogger, PlanarRedisStreamHook>(logger);
        LoadSystemHook<TLogger, PlanarRedisPubSubHook>(logger);
        LoadSystemHook<TLogger, PlanarTelegramHook>(logger);
    }

    private static void LoadSystemHook<TLogger, THook>(ILogger<TLogger> logger)
        where THook : BaseSystemHook
    {
        try
        {
            var instance = Activator.CreateInstance<THook>();
            var wrapper = HookWrapper.CreateInternal(instance, logger);
            var result = MonitorHooks.TryAdd(instance.Name, wrapper);

            if (result)
            {
                logger.LogInformation("add system monitor hook {Name}", instance.Name);
            }
            else
            {
                logger.LogError("fail to add system monitor hook {Name}. already contains monitor with this name", instance.Name);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "fail to load system hook {Name}", typeof(THook).Name);
        }
    }

    public static string GetJobFolder(string? folder)
    {
        var path =
            string.IsNullOrWhiteSpace(folder) ?
            FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs) :
            FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs, folder);

        return path;
    }

    public static string GetJobsFolder()
    {
        var path = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs);
        return path;
    }

    public static string GetJobRelativePath(string? fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath)) { return string.Empty; }
        var baseDir = GetJobsFolder();
        var relativePath =
            fullPath.Length == baseDir.Length ?
            string.Empty :
            fullPath[(baseDir.Length + 1)..];
        return relativePath;
    }

    public static string GetJobFilename(string? folder, string filename)
    {
        var path =
            string.IsNullOrWhiteSpace(folder) ?
                FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs) :
                FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs, folder);

        var fullname = Path.Combine(path, filename);
        return fullname;
    }

    public static bool IsJobFolderExists(string? folder)
    {
        var path = GetJobFolder(folder);
        var result = Directory.Exists(path);
        return result;
    }

    public static bool IsJobFileExists(string? folder, string filename)
    {
        if (string.IsNullOrWhiteSpace(folder)) { return IsJobFileExists(filename); }
        var fullname = GetJobFilename(folder, filename);
        var result = File.Exists(fullname);
        return result;
    }

    private static bool IsJobFileExists(string filename)
    {
        var jobsPath = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs);
        var fullname = Path.Combine(jobsPath, filename);
        var result = File.Exists(fullname);
        return result;
    }

    public static void ValidateJobFolderExists(string? folder)
    {
        if (!IsJobFolderExists(folder))
        {
            var path = GetJobFolder(folder);
            throw new PlanarException($"folder '{path}' is not exists. (node {Environment.MachineName})");
        }
    }

    public static void ValidateJobFileExists(string? folder, string filename)
    {
        if (!IsJobFileExists(folder, filename))
        {
            var path = GetJobFolder(folder);
            throw new PlanarException($"folder '{path}' does not have '{filename}' filename. (node {Environment.MachineName})");
        }
    }

    public static void ValidateJobFileExists(string filename)
    {
        if (!IsJobFileExists(filename))
        {
            throw new PlanarException($"filename '{filename}' does not exists in jobs folder on server. (node {Environment.MachineName})");
        }
    }

    public static int? GetExceptionCount(IJobExecutionContext? context)
    {
        if (context == null) { return null; }
        var metadata = context.Result as JobExecutionMetadata;
        var result = metadata?.Exceptions?.Count;
        return result;
    }

    public static int? GetEffectedRows(IJobExecutionContext? context)
    {
        if (context == null) { return null; }
        var metadata = context.Result as JobExecutionMetadata;
        var result = metadata?.EffectedRows.GetValueOrDefault();
        return result;
    }

    public static bool HasWarnings(IJobExecutionContext? context)
    {
        if (context == null) { return false; }
        var metadata = context.Result as JobExecutionMetadata;
        var result = metadata?.HasWarnings ?? false;
        return result;
    }

    public static void AddDisposeWarningToLog(ILogger logger)
    {
        lock (_locker)
        {
            if (_disposeFlag) { return; }
            logger.LogWarning("Execution of monitor events was canceled due to service dispose");
            _disposeFlag = true;
        }
    }
}