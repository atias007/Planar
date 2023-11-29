using System;

namespace Planar.API.Common.Entities;

public class MonitorSettingsInfo
{
    public int MaxAlertsPerMonitor { get; set; }
    public TimeSpan MaxAlertsPeriod { get; set; }
    public TimeSpan ManualMuteMaxPeriod { get; set; }
}