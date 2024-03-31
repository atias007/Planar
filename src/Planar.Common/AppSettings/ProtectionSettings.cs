using System;
using System.Diagnostics.CodeAnalysis;

namespace Planar.Common;

public class ProtectionSettings
{
    private int? _waitBeforeRestartMinutes;

    public int MaxMemoryUsage { get; set; }
    public bool RestartOnHighMemoryUsage { get; set; }
    public TimeSpan WaitBeforeRestart { get; set; }
    public string? RegularRestartExpression { get; set; }

    public int WaitBeforeRestartMinutes
    {
        get
        {
            _waitBeforeRestartMinutes ??= Convert.ToInt32(Math.Ceiling(WaitBeforeRestart.TotalMinutes));

            return _waitBeforeRestartMinutes.GetValueOrDefault();
        }
    }

    public bool HasRegularRestart => !string.IsNullOrWhiteSpace(RegularRestartExpression);
}