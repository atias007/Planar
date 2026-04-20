using System.ComponentModel.DataAnnotations.Schema;

namespace Planar.Service.Model;

[Table("MonitorActionsGroups")]
public class MonitorActionGroup
{
    public int MonitorId { get; set; }
    public int GroupId { get; set; }
}

[Table("MonitorActionsHooks")]
public class MonitorActionsHooks
{
    public int MonitorId { get; set; }
    public string Hook { get; set; } = null!;
}