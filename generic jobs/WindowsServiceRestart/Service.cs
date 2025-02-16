using Common;
using Microsoft.Extensions.Configuration;

namespace WindowsServiceRestart;

internal class Service : BaseOperation, IService, INamedCheckElement, IVetoEntity, IIntervalEntity
{
    public Service(IConfigurationSection section, Defaults defaults) : base(section, defaults)
    {
        Name = section.GetValue<string?>("name") ?? string.Empty;
        HostGroupName = section.GetValue<string?>("host group name");
        IgnoreDisabled = section.GetValue<bool?>("ignore disabled") ?? true;
        StopTimeout = section.GetValue<TimeSpan?>("stop timeout") ?? TimeSpan.FromSeconds(30);
        StartTimeout = section.GetValue<TimeSpan?>("start timeout") ?? TimeSpan.FromSeconds(30);
        Interval = section.GetValue<TimeSpan?>("interval");
        KillProcess = section.GetValue<bool?>("kill process") ?? false;
    }

    // CLONE //
    public Service(Service service) : base(service)
    {
        Name = service.Name;
        HostGroupName = service.HostGroupName;
        IgnoreDisabled = service.IgnoreDisabled;
        StopTimeout = service.StopTimeout;
        StartTimeout = service.StartTimeout;
        Interval = service.Interval;
        KillProcess = service.KillProcess;
    }

    public string Name { get; }
    public string? HostGroupName { get; }
    public bool IgnoreDisabled { get; }
    public TimeSpan StopTimeout { get; }
    public TimeSpan StartTimeout { get; }
    public TimeSpan? Interval { get; }
    public bool KillProcess { get; }
    public string Key => Name;

    //// --------------------------------------- ////

    public string? Host { get; set; }
}