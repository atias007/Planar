using System.Collections.Generic;

namespace Planar.Hook
{
    internal class MonitorSystemDetails : Monitor, IMonitorSystemDetails
    {
        public string MessageTemplate { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

#if NETSTANDARD2_0
        public IReadOnlyDictionary<string, string> MessagesParameters { get; set; } = new Dictionary<string, string>();

        internal void AddMessageParameter(string key, string value)
        {
            var messagesParameters = (Dictionary<string, string>)MessagesParameters;
            messagesParameters.Add(key, value);
        }

#else
        public IReadOnlyDictionary<string, string?> MessagesParameters { get; set; } = new Dictionary<string, string?>();

        internal void AddMessageParameter(string key, string? value)
        {
            var messagesParameters = (Dictionary<string, string?>)MessagesParameters;
            messagesParameters.Add(key, value);
        }
#endif
    }
}