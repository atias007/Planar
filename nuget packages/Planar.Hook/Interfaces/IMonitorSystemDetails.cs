using System.Collections.Generic;

namespace Planar.Hook
{
    public interface IMonitorSystemDetails : IMonitor
    {
        string MessageTemplate { get; }
        string Message { get; }
#if NETSTANDARD2_0
        IReadOnlyDictionary<string, string> MessagesParameters { get; }
#else
        IReadOnlyDictionary<string, string?> MessagesParameters { get; }
#endif
    }
}