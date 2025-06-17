using System.ComponentModel.DataAnnotations.Schema;

namespace Planar.Service.Model;

[Table("MonitorActionsGroups")]
public class MonitorActionGroup
{
    public int MonitorId { get; set; }
    public int GroupId { get; set; }
}