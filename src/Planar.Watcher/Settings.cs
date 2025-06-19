namespace Planar.Watcher;

public class Settings
{
    public required string ServiceName { get; set; }
    public required string Host { get; set; }
    public bool IgnoreDisabledService { get; set; }
    public required TimeSpan StartServiceTimeout { get; set; }
    public required TimeSpan StopPendingServiceTimeout { get; set; }
    public bool KillPendingServiceProcess { get; set; }
}