using System;
using System.Text.Json.Serialization;

namespace Planar.API.Common.Entities
{
    public class ReportsStatus
    {
        public string Period { get; set; } = null!;
        public bool Enabled { get; set; }
        public string? Group { get; set; }
        public DateTime? NextRunning { get; set; }
    }
}