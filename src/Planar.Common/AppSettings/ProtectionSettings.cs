using System;

namespace Planar.Common;

public class ProtectionSettings
{
    private int? _waitBeforeRestartMinutes;

    public int MaxMemoryUsage { get; set; }
    public bool RestartOnHighMemoryUsage { get; set; }
    public TimeSpan WaitBeforeRestart { get; set; }

    public int WaitBeforeRestartMinutes
    {
        get
        {
            _waitBeforeRestartMinutes ??= Convert.ToInt32(Math.Ceiling(WaitBeforeRestart.TotalMinutes));

            return _waitBeforeRestartMinutes.GetValueOrDefault();
        }
    }
}