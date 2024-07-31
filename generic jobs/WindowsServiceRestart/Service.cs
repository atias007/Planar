using Common;
using Microsoft.Extensions.Configuration;

namespace WindowsServiceRestart;

internal class Service(IConfigurationSection section, Defaults defaults) : BaseDefault(section, defaults), IService, INamedCheckElement
{
    public string Name { get; } = section.GetValue<string?>("name") ?? string.Empty;
    public bool Active { get; } = section.GetValue<bool?>("active") ?? true;
    public bool IgnoreDisabled { get; } = section.GetValue<bool?>("ignore disabled") ?? true;
    public IEnumerable<string> Hosts { get; private set; } = section.GetSection("hosts").Get<string[]>() ?? [];
    public TimeSpan Timeout { get; } = section.GetValue<TimeSpan?>("timeout") ?? TimeSpan.FromSeconds(60);
    public TimeSpan? Interval { get; } = section.GetValue<TimeSpan?>("interval");
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