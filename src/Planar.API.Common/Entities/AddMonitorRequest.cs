namespace Planar.API.Common.Entities;

public class AddMonitorRequest : MonitorRequest
{
    public string GroupName { get; set; } = null!;
}