using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common.Exceptions;
using Planar.Hooks;
using Planar.Monitor.Hook;
using Planar.Service.Monitor;
using Quartz;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace Planar.Service.General
{
    public static class ServiceUtil
    {
        internal static ConcurrentDictionary<string, HookWrapper> MonitorHooks { get; private set; } = new();
        private static bool _disposeFlag;
        private static readonly object _locker = new();

        public static IEnumerable<string> JobTypes => new[]
        {
            nameof(PlanarJob),
            nameof(ProcessJob),
            nameof(SqlJob),
            nameof(RestJob)
        };

        internal static string GenerateId()
        {
            var id = Path.GetRandomFileName().Replace(".", string.Empty);
            return id;
        }

        public static void LoadMonitorHooks<T>(ILogger<T> logger)
        {
            var path = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.MonitorHooks);

            if (!Directory.Exists(path))
            {
                logger.LogWarning("monitor hooks path {Path} could not be found. Service does not have any monitor", path);
                return;
            }

            logger.LogInformation("load monitor hooks at node {MachineName}", Environment.MachineName);

            MonitorHooks.Clear();
            LoadSystemHooks(logger);
            var directories = Directory.GetDirectories(path);
            foreach (var dir in directories)
            {
                var files = Directory.GetFiles(dir, "*.exe");

                foreach (var f in files)
                {
                    var success = LoadHook(logger, f);
                    if (success) { break; }
                }
            }
        }

        private static bool LoadHook<T>(ILogger<T> logger, string filename)
        {
            var validator = new HookValidator(filename, logger);
            if (!validator.IsValid) { return false; }

            var wrapper = HookWrapper.CreateExternal(filename);
            var result = MonitorHooks.TryAdd(validator.Name, wrapper);

            if (result)
            {
                logger.LogInformation("add monitor hook {Name} from file {Filename}", validator.Name, filename);
            }
            else
            {
                logger.LogError("fail to add monitor hook {Name} from file {Filename}. already contains monitor with this name", validator.Name, filename);
            }

            return result;
        }

        private static void LoadSystemHooks<TLogger>(ILogger<TLogger> logger)
        {
            LoadSystemHook<TLogger, PlanarRestHook>(logger);
            LoadSystemHook<TLogger, PlanarSmtpHook>(logger);
            LoadSystemHook<TLogger, PlanarLogHook>(logger);
        }

        private static void LoadSystemHook<TLogger, THook>(ILogger<TLogger> logger)
            where THook : BaseHook
        {
            try
            {
                var instance = Activator.CreateInstance<THook>();
                var wrapper = HookWrapper.CreateInternal(instance);
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
            var path = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs, folder);
            return path;
        }

        public static string GetJobsFolder()
        {
            var path = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs);
            return path;
        }

        public static string GetJobFilename(string? folder, string filename)
        {
            var path = GetJobFolder(folder);
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
            var fullname = GetJobFilename(folder, filename);
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

        public static int? GetEffectedRows(IJobExecutionContext? context)
        {
            if (context == null) { return null; }
            var metadata = context.Result as JobExecutionMetadata;
            var result = metadata?.EffectedRows.GetValueOrDefault();
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
}