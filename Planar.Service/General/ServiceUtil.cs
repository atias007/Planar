using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.Monitor;
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
            var path = Path.Combine(FolderConsts.BasePath, FolderConsts.Data, FolderConsts.MonitorHooks);

            if (Directory.Exists(path) == false)
            {
                logger.LogWarning("MonitorHooks path {Path} could not be found. Service does not have any monitor", path);
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
                            logger.LogInformation("Add MonitorHook '{@name}' from type '{@FullName}'", name, t.FullName);
                        }
                    }
                }

                if (hasHook == false)
                {
                    assemblyContext.Unload();
                }
            }
        }
    }
}