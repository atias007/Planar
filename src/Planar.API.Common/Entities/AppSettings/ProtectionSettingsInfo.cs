using System;

namespace Planar.API.Common.Entities
{
    public class ProtectionSettingsInfo
    {
        public int MaxMemoryUsage { get; set; }
        public bool RestartOnHighMemoryUsage { get; set; }
        public TimeSpan WaitBeforeRestart { get; set; }
        public string? RegularRestartExpression { get; set; }
    }
}