using System;
using System.Collections.Generic;

namespace Planar.Monitor.Hook
{
    public interface IMonitorSystemDetails : IMonitor
    {
        string MessageTemplate { get; }
        string Message { get; }
        IReadOnlyDictionary<string, string?> MessagesParameters { get; }
    }
}