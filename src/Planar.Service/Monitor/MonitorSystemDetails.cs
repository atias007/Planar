using System.Collections.Generic;

namespace Planar.Service.Monitor;

public class MonitorSystemDetails : Monitor
{
    public string MessageTemplate { get; set; } = null!;

    public string Message { get; set; } = null!;

    public Dictionary<string, string?> MessagesParameters { get; set; } = new();
}