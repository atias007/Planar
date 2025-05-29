using System.Collections.Generic;
using System.Linq;

namespace Planar.Hook
{
    internal class Monitor : IMonitor
    {
        private readonly List<IMonitorGroup> _groups = new List<IMonitorGroup>();
        public string Environment { get; set; } = string.Empty;

        public int EventId { get; set; }

        public string EventTitle { get; set; } = string.Empty;

        public string MonitorTitle { get; set; } = string.Empty;

        public IEnumerable<IMonitorGroup> Groups => _groups;

        public IEnumerable<IMonitorUser> Users => Groups.SelectMany(g => g.Users);
#if NETSTANDARD2_0

        public IReadOnlyDictionary<string, string> GlobalConfig { get; set; } = new Dictionary<string, string>();
#else
        public IReadOnlyDictionary<string, string?> GlobalConfig { get; set; } = new Dictionary<string, string?>();
#endif

#if NETSTANDARD2_0
        public string Exception { get; set; }
        public string MostInnerException { get; set; }
        public string MostInnerExceptionMessage { get; set; }
#else
        public string? Exception { get; set; }
        public string? MostInnerException { get; set; }
        public string? MostInnerExceptionMessage { get; set; }
#endif

        internal void AddGroup(IMonitorGroup group)
        {
            _groups.Add(group);
        }

        internal void ClearGroups()
        {
            _groups.Clear();
        }

#if NETSTANDARD2_0

        internal void AddGlobalConfig(string key, string value)
        {
            var globalConfig = (Dictionary<string, string>)GlobalConfig;
            globalConfig.Add(key, value);
        }

#else

        internal void AddGlobalConfig(string key, string? value)
        {
            var globalConfig = (Dictionary<string, string?>)GlobalConfig;
            globalConfig.Add(key, value);
        }
#endif
    }
}