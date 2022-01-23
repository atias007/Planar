using Microsoft.Extensions.Logging;
using Planner.Common;
using Planner.MonitorHook;
using Planner.Service.Monitor;
using Quartz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;

namespace Planner.Service.General
{
    internal static class ServiceUtil
    {
        internal static Dictionary<string, MonitorHookFactory> MonitorHooks { get; private set; } = new();

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
            var path = AppDomain.CurrentDomain.BaseDirectory;
            path = Path.Combine(path, "MonitorHooks");

            if (Directory.Exists(path) == false)
            {
                logger.LogWarning("MonitorHooks path could not be found. Service does not have any monitor");
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

                foreach (var f in files)
                {
                    var assembly = AssemblyLoader.LoadFromAssemblyPath(f, assemblyContext);
                    var types = assembly.GetTypes().Where(t => t.IsInterface == false && t.IsAbstract == false);
                    foreach (var t in types)
                    {
                        var isHook = typeof(IMonitorHook).IsAssignableFrom(t);
                        if (isHook)
                        {
                            hasHook = true;
                            var name = new DirectoryInfo(dir).Name;
                            var hook = new MonitorHookFactory { Name = name, Type = t, AssemblyContext = assemblyContext };
                            MonitorHooks.Add(name, hook);
                            logger.LogInformation($"Add MonitorHook '{name}' from type '{t.FullName}'");
                        }
                    }
                }

                if (hasHook == false)
                {
                    assemblyContext.Unload();
                }
            }
        }

        internal static SortedDictionary<string, string> ConvertJobDataMapToDictionary(JobDataMap dataMap)
        {
            if (dataMap == null) { return null; }

            var result = new Dictionary<string, string>(dataMap.Count);
            var arr = new KeyValuePair<string, object>[dataMap.Count];
            dataMap.CopyTo(arr, 0);
            foreach (var item in arr.Where(a => a.Key.StartsWith(Consts.ConstPrefix) == false && a.Key.StartsWith(Consts.QuartzPrefix) == false))
            {
                result.Add(item.Key, Convert.ToString(item.Value));
            }

            return new SortedDictionary<string, string>(result);
        }
    }
}