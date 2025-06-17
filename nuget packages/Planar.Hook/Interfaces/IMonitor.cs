using System.Collections.Generic;

namespace Planar.Hook
{
    public interface IMonitor
    {
#if NETSTANDARD2_0
        IReadOnlyDictionary<string, string> GlobalConfig { get; }
        string Exception { get; }
        string MostInnerException { get; }
        string MostInnerExceptionMessage { get; }
#else
        IReadOnlyDictionary<string, string?> GlobalConfig { get; }
        string? Exception { get; }
        string? MostInnerException { get; }
        string? MostInnerExceptionMessage { get; }
#endif
        int EventId { get; }
        string EventTitle { get; }
        string MonitorTitle { get; }
        IEnumerable<IMonitorGroup> Groups { get; }
        IEnumerable<IMonitorUser> Users { get; }
        string Environment { get; set; }
    }
}