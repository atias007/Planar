namespace Planar.Service.Model;

public partial class MonitorAction
{
    public bool MonitorForJob => JobName != null && JobGroup != null;
}