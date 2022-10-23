using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Planar.Common
{
    public static class Global
    {
        public static Dictionary<string, string> GlobalConfig { get; private set; }

        public static LogLevel LogLevel { get; set; }

        public static string Environment { get; set; }

        public static void SetGlobalConfig(Dictionary<string, string> config)
        {
            GlobalConfig = config;
        }

        public static void Clear()
        {
            GlobalConfig = null;
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