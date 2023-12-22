using System.Collections.Generic;

namespace Planar.Hook
{
    internal class Monitor : IMonitor
    {
        public string Environment { get; set; } = string.Empty;

        public int EventId { get; set; }

        public string EventTitle { get; set; } = string.Empty;

        public string MonitorTitle { get; set; } = string.Empty;

        public IMonitorGroup Group { get; set; } = new Group();

        public IEnumerable<IMonitorUser> Users { get; set; } = new List<User>();

        public IReadOnlyDictionary<string, string?> GlobalConfig { get; set; } = new Dictionary<string, string?>();

        public string? Exception { get; set; }

        public string? MostInnerException { get; set; }

        public string? MostInnerExceptionMessage { get; set; }

        internal void AddUser(IMonitorUser user)
        {
            if (user is User castUser)
            {
                var users = (List<User>)Users;
                users.Add(castUser);
            }
        }

        internal void AddGlobalConfig(string key, string? value)
        {
            var globalConfig = (Dictionary<string, string?>)GlobalConfig;
            globalConfig.Add(key, value);
        }
    }
}