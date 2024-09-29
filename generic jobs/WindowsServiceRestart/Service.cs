using Common;
using Microsoft.Extensions.Configuration;

namespace WindowsServiceRestart;

internal class Service : BaseDefault, IService, INamedCheckElement, IVetoEntity, IIntervalEntity
{
    public Service(IConfigurationSection section, Defaults defaults) : base(section, defaults)
    {
        Name = section.GetValue<string?>("name") ?? string.Empty;
        HostGroupName = section.GetValue<string?>("host group name");
        IgnoreDisabled = section.GetValue<bool?>("ignore disabled") ?? true;
        Timeout = section.GetValue<TimeSpan?>("timeout") ?? TimeSpan.FromSeconds(60);
        Interval = section.GetValue<TimeSpan?>("interval");
    }

    // CLONE //
    public Service(Service service) : base(service)
    {
        Name = service.Name;
        HostGroupName = service.HostGroupName;
        IgnoreDisabled = service.IgnoreDisabled;
        Timeout = service.Timeout;
        Interval = service.Interval;
    }

    public string Name { get; }
    public string? HostGroupName { get; }
    public bool IgnoreDisabled { get; }
    public TimeSpan Timeout { get; }
    public TimeSpan? Interval { get; }
    public string Key => Name;

    //// --------------------------------------- ////

    public string? Host { get; set; }

    public bool Veto { get; set; }

    public string? VetoReason { get; set; }
}