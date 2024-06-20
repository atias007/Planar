using Common;
using Microsoft.Extensions.Configuration;

namespace WindowsServiceCheck;

internal class Service(IConfigurationSection section) : BaseDefault(section), IService, INamedCheckElement
{
    public string Name { get; } = section.GetValue<string?>("name") ?? string.Empty;
    public bool Active { get; } = section.GetValue<bool?>("active") ?? true;
    public bool IgnoreDisabled { get; } = section.GetValue<bool?>("ignore disabled") ?? true;
    public bool StartService { get; } = section.GetValue<bool?>("start service") ?? true;
    public bool AutomaticStart { get; } = section.GetValue<bool?>("automatic start") ?? true;
    public IEnumerable<string> Hosts { get; private set; } = section.GetSection("hosts").Get<string[]>() ?? [];
    public TimeSpan StartServiceTimeout { get; } = section.GetValue<TimeSpan?>("start service timeout") ?? TimeSpan.FromSeconds(30);
    public string Key => Name;

    public void SetHosts(IEnumerable<string> hosts)
    {
        Hosts = hosts;
    }

    public void ClearInvalidHosts()
    {
        Hosts = Hosts.Where(f => !string.IsNullOrWhiteSpace(f));
    }
}