using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.Exceptions;
using Planar.Service.Monitor;
using Quartz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;

namespace Planar.Service.General
{
    public static class ServiceUtil
    {
        public static Dictionary<string, MonitorHookFactory> MonitorHooks { get; private set; } = new();
        private static bool _disposeFlag;
        private static readonly object _locker = new();

        internal static string GenerateId()
        {
            var id = Path.GetRandomFileName().Replace(".", string.Empty);
            return id;
        }

        private static void ClearMonitorHooks()
        {
            MonitorHooks.Clear();
            var contexts = AssemblyLoadContext.All.Where(c => c.IsCollectible && c.Name.StartsWith("MonitorHook_"));
            foreach (var c in contexts)
            {
                c.Unload();
            }
        }

        internal static void LoadMonitorHooks<T>(ILogger<T> logger)
        {
            var path = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.MonitorHooks);

            if (Directory.Exists(path) == false)
            {
                logger.LogWarning("monitor hooks path {Path} could not be found. Service does not have any monitor", path);
                return;
            }

            ClearMonitorHooks();
            var directories = Directory.GetDirectories(path);
            foreach (var dir in directories)
            {
                var folder = Path.GetDirectoryName(dir);
                var assemblyContext = AssemblyLoader.CreateAssemblyLoadContext($"MonitorHook_{folder}", true);
                var files = Directory.GetFiles(dir, "*.dll");
                var hasHook = false;
                const string baseClassName = "Planar.MonitorHook.BaseMonitorHook";

                foreach (var f in files)
                {
                    var assembly = AssemblyLoader.LoadFromAssemblyPath(f, assemblyContext);
                    var types = assembly.GetTypes().Where(t => t.IsInterface == false && t.IsAbstract == false);
                    foreach (var t in types)
                    {
                        var isHook = t.BaseType.FullName == baseClassName;
                        if (isHook)
                        {
                            hasHook = true;
                            var name = new DirectoryInfo(dir).Name;
                            var hook = new MonitorHookFactory { Name = name, Type = t, AssemblyContext = assemblyContext };
                            MonitorHooks.Add(name, hook);
                            logger.LogInformation("Add MonitorHook '{Name}' from type '{FullName}'", name, t.FullName);
                        }
                    }
                }

                if (hasHook == false)
                {
                    assemblyContext.Unload();
                }
            }
        }

        public static string GetJobFolder(string folder)
        {
            var path = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs, folder);
            return path;
        }

        public static string GetJobsFolder()
        {
            var path = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs);
            return path;
        }

        public static string GetJobFilename(string folder, string filename)
        {
            var path = GetJobFolder(folder);
            var fullname = Path.Combine(path, filename);
            return fullname;
        }

        public static bool IsJobFolderExists(string folder)
        {
            var path = GetJobFolder(folder);
            var result = Directory.Exists(path);
            return result;
        }

        public static bool IsJobFileExists(string folder, string filename)
        {
            var fullname = GetJobFilename(folder, filename);
            var result = File.Exists(fullname);
            return result;
        }

        public static void ValidateJobFolderExists(string folder)
        {
            if (!IsJobFolderExists(folder))
            {
                var path = GetJobFolder(folder);
                throw new PlanarException($"folder '{path}' is not exists. (node {Environment.MachineName})");
            }
        }

        public static void ValidateJobFileExists(string folder, string filename)
        {
            if (!IsJobFileExists(folder, filename))
            {
                var path = GetJobFolder(folder);
                throw new PlanarException($"folder '{path}' does not have '{filename}' filename. (node {Environment.MachineName})");
            }
        }

        public static int? GetEffectedRows(IJobExecutionContext context)
        {
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