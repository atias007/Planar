using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Planar.Common
{
    public static class Global
    {
        public static Dictionary<string, string> Parameters { get; set; }

        public static IServiceProvider ServiceProvider { get; set; }

        public static string Environment { get; set; }

        public static ILogger<T> GetLogger<T>()
        {
            var logger = ServiceProvider.GetService(typeof(ILogger<T>)) as ILogger<T>;
            return logger;
        }

        public static ILogger GetLogger(Type type)
        {
            Type generic = typeof(ILogger<>);
            var loggerType = generic.MakeGenericType(type);
            var logger = ServiceProvider.GetService(loggerType) as ILogger;
            return logger;
        }

        public static void Clear()
        {
            Parameters = null;
            ServiceProvider = null;
        }

        public static SortedDictionary<string, string> ConvertDataMapToDictionary(JobDataMap map)
        {
            if (map == null)
            {
                return null;
            }

            var dic = map
                .Where(k => k.Key.StartsWith(Consts.ConstPrefix) == false && k.Key.StartsWith(Consts.QuartzPrefix) == false)
                .OrderBy(k => k.Key)
                .ToDictionary(k => k.Key, v => Convert.ToString(v.Value));

            var result = new SortedDictionary<string, string>(dic);
            return result;
        }
    }
}