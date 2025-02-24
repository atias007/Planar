using Common;
using Microsoft.Extensions.Configuration;

namespace WindowsServiceCheck;

internal class Service : BaseDefault, INamedCheckElement, IVetoEntity
{
    public Service(IConfigurationSection section, Defaults defaults) : base(section, defaults)
    {
        Name = section.GetValue<string?>("name") ?? string.Empty;
        HostGroupName = section.GetValue<string?>("host group name");
        IgnoreDisabled = section.GetValue<bool?>("ignore disabled") ?? true;
        StartService = section.GetValue<bool?>("start service") ?? true;
        AutoStartMode = section.GetValue<bool?>("auto start mode") ?? true;
        StartServiceTimeout = section.GetValue<TimeSpan?>("start service timeout") ?? TimeSpan.FromSeconds(30);
        StopPendingServiceTimeout = section.GetValue<TimeSpan?>("stop pending service timeout") ?? TimeSpan.FromMinutes(1);
        KillPendingServiceProcess = section.GetValue<bool?>("kill pending service process") ?? true;
    }

    public Service(Service service) : base(service)
    {
        Name = service.Name;
        HostGroupName = service.HostGroupName;
        IgnoreDisabled = service.IgnoreDisabled;
        StartService = service.StartService;
        AutoStartMode = service.AutoStartMode;
        StartServiceTimeout = service.StartServiceTimeout;
        StopPendingServiceTimeout = service.StopPendingServiceTimeout;
        KillPendingServiceProcess = service.KillPendingServiceProcess;
    }

    public string Name { get; }
    public string? HostGroupName { get; }
    public bool IgnoreDisabled { get; }
    public bool StartService { get; }
    public bool AutoStartMode { get; }
    public TimeSpan StopPendingServiceTimeout { get; set; }
    public bool KillPendingServiceProcess { get; set; }
    public TimeSpan StartServiceTimeout { get; }
    public string Key => Name;

    //// --------------------------------------- ////

    public ServiceResult Result { get; } = new();

    public string? Host { get; set; }
}