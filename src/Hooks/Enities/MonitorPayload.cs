namespace Planar.Hooks.Enities;

internal class MonitorPayload
{
    public object Message { get; set; } = null!;
    public DateTime Created { get; set; } = DateTime.Now;
    public string Version { get; set; } = "1.0.0";
    public string SourceUrl { get; set; } = null!;
    public string HookName { get; set; } = null!;
}