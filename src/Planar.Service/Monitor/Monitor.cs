using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Planar.Service.Monitor;

public class Monitor
{
    public int EventId { get; set; }
    public string EventTitle { get; set; } = null!;
    public string MonitorTitle { get; set; } = null!;
    public IEnumerable<MonitorGroup> Groups { get; set; } = [];
    public Dictionary<string, string?> GlobalConfig { get; set; } = [];
    public string? Exception { get; set; }
    public string? MostInnerException { get; set; }
    public string? MostInnerExceptionMessage { get; set; }
    public string Environment { get; set; } = null!;

    [JsonIgnore]
    public IEnumerable<MonitorUser> Users => Groups.SelectMany(g => g.Users).DistinctBy(u => u.Id);
}