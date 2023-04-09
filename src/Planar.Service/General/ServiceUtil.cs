using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Common.Exceptions;
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
        public static ConcurrentDictionary<string, MonitorHookFactory> MonitorHooks { get; private set; } = new();
        private static bool _disposeFlag;
        private static readonly object _locker = new();
        private const string _monitorHookAssemblyContextName = "MonitorHook_";
        private const string _monitorHookBaseClassName = "Planar.Monitor.Hook.BaseHook";

        public static IEnumerable<string> JobTypes => new[] { nameof(PlanarJob), nameof(ProcessJob), nameof(SqlJob) };

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
                        hasHook = LoadHook(logger, dir, assemblyContext, t);
                    }
                }

                if (!hasHook)
                {
                    assemblyContext.Unload();
                }
            }
        }

        private static bool LoadHook<T>(ILogger<T> logger, string dir, AssemblyLoadContext assemblyContext, Type t)
        {
            var name = new DirectoryInfo(dir).Name;
            var hook = new MonitorHookFactory { Name = name, Type = t, AssemblyContext = assemblyContext };
            var result = MonitorHooks.TryAdd(name, hook);

            if (result)
            {
                logger.LogInformation("Add MonitorHook '{Name}' from type '{FullName}'", name, t.FullName);
            }

            return result;
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

        public static void AddDisposeWarningToLog<T>(ILogger<T> logger)
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