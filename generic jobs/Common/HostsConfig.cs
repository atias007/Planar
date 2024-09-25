using Microsoft.Extensions.Configuration;

namespace Common;

public class HostsConfig(IConfigurationSection section)
{
    public string GroupName { get; private set; } = section.GetValue<string>("group name") ?? string.Empty;
    public IEnumerable<string> Hosts { get; private set; } = section.GetRequiredSection("hosts").Get<string[]>()?.ToList()?.Distinct() ?? [];

    public IEnumerable<Host> VetoHosts(Action<Host> veto)
    {
        var allHosts = Hosts.Select(h => new Host(h)).ToList();
        allHosts.ForEach(h => veto(h));
        Hosts = allHosts.Where(h => !h.Veto).Select(h => h.Name);
        return allHosts.Where(h => h.Veto);
    }
}