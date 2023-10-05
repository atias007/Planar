using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Common.Exceptions;
using Planar.Hooks;
using Planar.Monitor.Hook;
using Planar.Service.Monitor;
using Quartz;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;

namespace Planar.Service.General
{
    public static class ServiceUtil
    {
        public static ConcurrentDictionary<string, BaseHook> MonitorHooks { get; private set; } = new();
        private static bool _disposeFlag;
        private static readonly object _locker = new();
        private const string _monitorHookAssemblyContextName = "MonitorHook_";
        private const string _monitorHookBaseClassName = "Planar.Monitor.Hook.BaseHook";

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

        private static void ClearMonitorHooks()
        {
            MonitorHooks.Clear();
            var contexts = AssemblyLoadContext.All
                .Where(c => c.IsCollectible && c.Name != null && c.Name.StartsWith(_monitorHookAssemblyContextName));

            foreach (var c in contexts)
            {
                c.Unload();
            }
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

            ClearMonitorHooks();
            LoadSystemHooks(logger);
            var directories = Directory.GetDirectories(path);
            foreach (var dir in directories)
            {
                var folder = Path.GetDirectoryName(dir);
                var assemblyContext = AssemblyLoader.CreateAssemblyLoadContext($"{_monitorHookAssemblyContextName}{folder}", true);
                var files = Directory.GetFiles(dir, "*.dll");
                var hasHook = false;

                foreach (var f in files)
                {
                    var types = GetHookTypesFromFile(assemblyContext, f);
                    foreach (var t in types)
                    {
                        hasHook = LoadHook(logger, dir, t);
                    }
                }

                if (!hasHook)
                {
                    assemblyContext.Unload();
                }
            }
        }

        private static bool LoadHook<T>(ILogger<T> logger, string dir, Type t)
        {
            if (Activator.CreateInstance(t) is not BaseHook instance)
            {
                var name = new DirectoryInfo(dir).Name;
                logger.LogWarning("Fail to add monitor hook from directory {Name} with type {FullName}", name, t.FullName);
                return false;
            }

            var result = MonitorHooks.TryAdd(instance.Name, instance);

            if (result)
            {
                logger.LogInformation("Add monitor hook {Name} from type {FullName}", instance.Name, t.FullName);
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
            var instance = Activator.CreateInstance<THook>();
            var result = MonitorHooks.TryAdd(instance.Name, instance);

            if (result)
            {
                logger.LogInformation("Add system monitor hook {Name}", instance.Name);
            }
            else
            {
                logger.LogError("Fail to add system monitor hook {Name}", instance.Name);
            }
        }

        private static IEnumerable<Type> GetHookTypesFromFile(AssemblyLoadContext assemblyContext, string file)
        {
            var result = new List<Type>();
            IEnumerable<Type> allTypes = new List<Type>();

            try
            {
                var assembly = AssemblyLoader.LoadFromAssemblyPath(file, assemblyContext);
                if (assembly == null) { return allTypes; }
                allTypes = assembly.GetTypes().Where(t => !t.IsInterface && !t.IsAbstract);
            }
            catch
            {
                return result;
            }

            foreach (var t in allTypes)
            {
                try
                {
                    var isHook = t.BaseType != null && t.BaseType.FullName == _monitorHookBaseClassName;
                    if (isHook)
                    {
                        result.Add(t);
                    }
                }
                catch
                {
                    // *** DO NOTHING --> SKIP TYPE *** //
                }
            }

            return result;
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