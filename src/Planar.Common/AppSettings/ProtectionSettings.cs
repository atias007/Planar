using System;

namespace Planar.Common;

public class ProtectionSettings
{
    public int MaxMemoryUsage { get; set; }
    public bool RestartOnHighMemoryUsage { get; set; }
    public TimeSpan WaitBeforeRestart { get; set; }
}