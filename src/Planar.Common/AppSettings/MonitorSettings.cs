using System;

namespace Planar.Common;

public class MonitorSettings
{
    public int MaxAlertsPerMonitor { get; set; }
    public TimeSpan MaxAlertsPeriod { get; set; }
    public TimeSpan ManualMuteMaxPeriod { get; set; }
}