namespace Planar.API.Common.Entities;

public class UpdateMonitorRequest : MonitorRequest
{
    public int Id { get; set; }

    public bool Active { get; set; }
}