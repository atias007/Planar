namespace Planar.API.Common.Entities;

public class MonitorHookRequest
{
    public string Hook { get; set; } = null!;
    public int MonitorId { get; set; }
}