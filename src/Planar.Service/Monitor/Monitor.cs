using System.Collections.Generic;

namespace Planar.Service.Monitor
{
    public class Monitor
    {
        public int EventId { get; set; }
        public string EventTitle { get; set; } = null!;
        public string MonitorTitle { get; set; } = null!;
        public MonitorGroup? Group { get; set; }
        public List<MonitorUser>? Users { get; set; } = new();
        public Dictionary<string, string?>? GlobalConfig { get; set; }
        public string? Exception { get; set; }
        public string? MostInnerException { get; set; }
        public string? MostInnerExceptionMessage { get; set; }
        public string Environment { get; set; } = null!;
    }
}