using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Version = System.Version;

namespace Planar.Common
{
    public static class Global
    {
        private static Version _version;

        public static Version Version
        {
            get
            {
                if (_version == null)
                {
                    _version = Assembly.GetExecutingAssembly().GetName().Version;
                }

                return _version;
            }
        }

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
                return new SortedDictionary<string, string>();
            }

            var dic = map
                .Where(k => !k.Key.StartsWith(Consts.ConstPrefix) && !k.Key.StartsWith(Consts.QuartzPrefix))
                .OrderBy(k => k.Key)
                .ToDictionary(k => k.Key, v => Convert.ToString(v.Value));

            var result = new SortedDictionary<string, string>(dic);
            return result;
        }
    }
}